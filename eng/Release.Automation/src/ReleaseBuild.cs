using System.Collections.Immutable;

using Microsoft.Extensions.Logging;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;

namespace Release.Automation;

sealed partial class ReleaseBuild : NukeBuild
{
    static readonly ILoggerFactory LoggerFactoryInstance = LoggerFactory.Create(builder =>
        builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.IncludeScopes = false;
        }));

    static readonly ILogger Logger = LoggerFactoryInstance.CreateLogger<ReleaseBuild>();

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    internal Configuration Configuration { get; set; } = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    internal Solution Solution { get; set; } = null!;

    [Parameter("Relative path to the central Versions.props file.")]
    internal string VersionsPropsFile { get; set; } = "Versions.props";

    [Parameter("Relative path to the directory where build artifacts (NuGet packages) will be outputted.")]
    internal string ArtifactsRelativeDirectory { get; set; } = "artifacts";

    [Parameter("Target release channel: Alpha (CI), Beta, Rc, or Stable (CD).")]
    internal ReleaseChannel Channel { get; set; } = ReleaseChannel.Alpha;

    [Parameter("Promote initial 0.x.y packages to 1.0.0 during release.")]
    internal bool PromoteToMajor { get; set; }

    [Parameter("List of project names to force republish if their dependencies were updated.")]
    internal string[] ForceProjects { get; set; } = [];

    [Parameter("List of tags to create and push (passed from previous build step).")]
    internal string[] ReleaseTags { get; set; } = [];

    AbsolutePath ArtifactsDirectory => RootDirectory / ArtifactsRelativeDirectory / Configuration;

    ImmutableList<ProjectReleaseContext> ProjectsToPack { get; set; } = [];

    internal Target Clean => _ => _
        .Before(Restore)
        .Executes(() => ArtifactsDirectory.CreateOrCleanDirectory());

    internal Target Restore => _ => _
        .Executes(() => DotNetTasks.DotNetRestore(s => s.SetProjectFile(Solution)));

    internal Target BumpVersions => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            ProjectsToPack = VersionBumpProcessor.Execute(
                [.. Solution.AllProjects],
                RootDirectory,
                (RelativePath)VersionsPropsFile,
                Channel,
                PromoteToMajor,
                ForceProjects,
                Logger);
        });

    internal Target Compile => _ => _
        .DependsOn(Restore, BumpVersions)
        .Executes(() =>
        {
            foreach (var ctx in ProjectsToPack)
            {
#pragma warning disable IDE0058 // Expression value is never used
                DotNetTasks.DotNetBuild(s => s
                    .SetProjectFile(ctx.Project)
                    .SetConfiguration(Configuration)
                    .SetVersion(ctx.CalculatedVersion)
                    .EnableNoRestore());
#pragma warning restore IDE0058 // Expression value is never used
            }
        });

    internal Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var ctx in ProjectsToPack)
            {
#pragma warning disable IDE0058 // Expression value is never used
                DotNetTasks.DotNetPack(s => s
                    .SetProject(ctx.Project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetVersion(ctx.CalculatedVersion)
                    .EnableNoRestore());
#pragma warning restore IDE0058 // Expression value is never used
            }

            var githubOutputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");

            if (!string.IsNullOrWhiteSpace(githubOutputFile))
            {
                var hasPackages = ProjectsToPack.Count > 0;
                var tagsList = string.Join(' ', ProjectsToPack.Select(ctx => $"{ctx.Project.Name}-v{ctx.CalculatedVersion}"));

                File.AppendAllLines(
                    githubOutputFile,
                    [
                        $"has_packages={(hasPackages ? "true" : "false")}",
                        $"release_tags={tagsList}"
                    ]);
            }
        });

    internal Target CreateGitTags => _ => _
        .DependsOn(Pack)
        .OnlyWhenStatic(() => Channel.IsExplicitRelease)
        .Executes(() =>
        {
            var tagsToCreate = ReleaseTags.Length > 0
                ? ReleaseTags
                : ProjectsToPack.Select(ctx => $"{ctx.Project.Name}-v{ctx.CalculatedVersion}");

            foreach (var tagName in tagsToCreate)
            {
                if (string.IsNullOrWhiteSpace(tagName) || GitTasksHelper.DoesGitTagExist(RootDirectory, tagName))
                {
                    continue;
                }

                try
                {
                    Logger.CreatingGitTag(tagName);
#pragma warning disable IDE0058 // Expression value is never used
                    GitTasks.Git($"tag {tagName}", RootDirectory);
                    GitTasks.Git($"push origin {tagName}", RootDirectory);
#pragma warning restore IDE0058 // Expression value is never used
                }
                catch (Exception ex) when (ex is not ArgumentException)
                {
                    Logger.GitTagCreationFailed(tagName, ex.Message);
                }
            }
        });

    internal static int Main() => Execute<ReleaseBuild>(x => x.Pack);
}
