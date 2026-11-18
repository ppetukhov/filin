using System.Text.RegularExpressions;

using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Git;

namespace Release.Automation;

static partial class GitTasksHelper
{
    [GeneratedRegex(
        @"^(?:(?<hash>[0-9a-f]{7,40})\s+)?(?:(?<type>fix|sec|security|chore|build)(?:\((?<scope>[^)]+)\))?:\s*)?.*?(?<keyword>security\s+patch|security\s+fix|cve-\d{4}-\d{4,7}|ghsa-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}|vulnerability|xss|sqli|csrf|rce).*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 2000)]
    private static partial Regex SecurityCommitRegex();

    [GeneratedRegex(
        @"(?:\A(?:[0-9a-f]{7,40}\s+)?[a-z]+(?:\([^)]+\))?!:\s*(?<headerDesc>[^\r\n]+))|(?:(?:\s+|^)BREAKING[\s-]CHANGE:\s*(?<footerDesc>[^\r\n]+))",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex BreakingCommitRegex();

    [GeneratedRegex(
        @"^(?:(?<hash>[0-9a-f]{7,40})\s+)?feat(?:\((?<scope>[^)]+)\))?(?<breaking>!)?:(?:\s+(?<description>.+))$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex FeatureCommitRegex();

    public static IReadOnlyCollection<Output> GitLog(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var range = GetRange(baselineTag);

        return GitTasks.Git($"log {range} --format=%s%x20%b -- \"{relativePath}\"", rootDirectory, logInvocation: false, logOutput: false);
    }

    public static int GetCommitCount(AbsolutePath rootDirectory)
    {
        var cnt = GitTasks.Git("rev-list --count HEAD", rootDirectory, logInvocation: false, logOutput: false).First().Text;

        return int.TryParse(cnt, out var count) ? count : 0;
    }

    public static IReadOnlyCollection<Output> GitShow(AbsolutePath rootDirectory, RelativePath relativePath, string tagName) =>
        GitTasks.Git($"show \"{tagName}:{relativePath}\"", rootDirectory, logInvocation: false, logOutput: false);

    public static IReadOnlyCollection<Output> GitDiff(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var range = GetRange(baselineTag);

        return GitTasks.Git($"diff -U0 {range} -- \"{relativePath}\"", rootDirectory, logInvocation: false, logOutput: false);
    }

    public static bool DoesGitTagExist(AbsolutePath rootDirectory, string tagName)
    {
        var output = GitTasks.Git($"tag -l \"{tagName}\"", rootDirectory, logInvocation: false, logOutput: false);
        return output.Count > 0;
    }

    /// <summary>
    /// Checks if the project has any breaking change commit markers since the baseline tag.
    /// </summary>
    public static bool HasBreakingCommitMarker(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var commits = GitLog(rootDirectory, relativePath, baselineTag);

        foreach (var line in commits)
        {
            if (line.Text.Length > 0 && BreakingCommitRegex().IsMatch(line.Text))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasCommitsSince(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var range = GetRange(baselineTag);

        var gitLog = GitTasks.Git($"log {range} -n 1 --oneline -- \"{relativePath}\"", rootDirectory, logInvocation: false, logOutput: false);

        return gitLog.Count > 0;
    }

    /// <summary>
    /// Checks if the project has any security fix commit markers since the baseline tag.
    /// </summary>
    public static bool HasSecurityFixMarker(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var commits = GitLog(rootDirectory, relativePath, baselineTag);

        foreach (var line in commits)
        {
            if (SecurityCommitRegex().IsMatch(line.Text))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the project has any feature commit markers ('feat:') since the baseline tag.
    /// </summary>
    public static bool HasFeatureCommitMarker(AbsolutePath rootDirectory, RelativePath relativePath, string? baselineTag)
    {
        var commits = GitLog(rootDirectory, relativePath, baselineTag);

        foreach (var line in commits)
        {
            if (line.Text.Length > 0 && FeatureCommitRegex().IsMatch(line.Text))
            {
                return true;
            }
        }

        return false;
    }
    static string GetRange(string? baselineTag) => string.IsNullOrEmpty(baselineTag) ? "HEAD" : $"{baselineTag}..HEAD";
}
