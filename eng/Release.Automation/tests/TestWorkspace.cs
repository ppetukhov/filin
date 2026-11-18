using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.VisualStudio.SolutionPersistence.Serializer;

using Nuke.Common.IO;

using Solution = Nuke.Common.ProjectModel.Solution;

namespace Release.Automation.Tests;

/// <summary>
/// Manages a temporary directory, Git repository state, and Solution creation for an individual test.
/// </summary>
public sealed class TestWorkspace : IDisposable
{
    public AbsolutePath RootDirectory { get; }

    TestWorkspace(AbsolutePath tempDir) => RootDirectory = tempDir;

    public static TestWorkspace CreateWorkspace()
    {
        var tempDir = (AbsolutePath)Path.Combine(
            Path.GetTempPath(),
            string.Format(CultureInfo.InvariantCulture, "test_env_{0}", Guid.NewGuid()));

        Directory.CreateDirectory(tempDir);

        return new(tempDir);
    }

    /// <summary>
    /// Initializes a Git repository in the workspace directory and sets local user identity.
    /// </summary>
    public async Task InitGitRepositoryAsync()
    {
        await RunGitCommandAsync("init");
        await RunGitCommandAsync("config user.name \"TestRunner\"");
        await RunGitCommandAsync("config user.email \"test@example.com\"");
    }

    /// <summary>
    /// Stages all changes in the workspace and creates a Git commit.
    /// </summary>
    public async Task CommitAllAsync(string message)
    {
        await RunGitCommandAsync("add .");
        await RunGitCommandAsync(string.Format(CultureInfo.InvariantCulture, "commit --allow-empty -m \"{0}\"", message));
    }

    /// <summary>
    /// Creates a file, commit and tags it.
    /// </summary>
    public async Task CreateFileCommitWithTagsAsync(string relativePath, string content, params string[] tags)
    {
        await CreateTextFileAsync(relativePath, content);
        await RunGitCommandAsync("add .");

        var tagsLine = tags.Length switch
        {
            0 => "test",
            1 => tags[0],
            _ => string.Join(", ", tags)
        };

        await RunGitCommandAsync(string.Format(CultureInfo.InvariantCulture, "commit -m \"Baseline commit for {0}\"", tagsLine));

        foreach (var tag in tags)
        {
            await RunGitCommandAsync(string.Format(CultureInfo.InvariantCulture, "tag \"{0}\"", tag));
        }
    }

    /// <summary>
    /// Creates a solution file structure on disk (.slnx) and constructs a NUKE <see cref="Solution"/> instance asynchronously.
    /// </summary>
    public async Task<Solution> CreateSolutionAsync(params (string Name, string RelativePath)[] projects)
    {
        var slnxPath = RootDirectory / "Test.slnx";

        var content = new StringBuilder();
        content.AppendLine("<Solution>");
        foreach (var (_, relativePath) in projects)
        {
            var normalizedPath = relativePath.Replace('/', '\\');
            content.AppendLine(CultureInfo.InvariantCulture, $"  <Project Path=\"{normalizedPath}\" />");
        }
        content.AppendLine("</Solution>");

        await File.WriteAllTextAsync(slnxPath, content.ToString());

        var serializer = SolutionSerializers.GetSerializerByMoniker(slnxPath)
                ?? throw new NotSupportedException("NotSupported format solution file.");

        var solutionModel = await serializer.OpenAsync(slnxPath, CancellationToken.None);

        return new Solution(solutionModel, slnxPath);
    }

    /// <summary>
    /// Creates a project file with specified content inside the temporary directory structure asynchronously.
    /// </summary>
    public async Task<AbsolutePath> CreateTextFileAsync(string relativePath, string content)
    {
        var path = RootDirectory / relativePath;
        Directory.CreateDirectory(path.Parent);

        await File.WriteAllTextAsync(path, content);

        return path;
    }

    async Task RunGitCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = RootDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git process.");
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Git command 'git {0}' failed: {1}", arguments, error));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(RootDirectory))
        {
            try
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
            catch { }
        }

        GC.SuppressFinalize(this);
    }
}
