using Microsoft.Extensions.Logging;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation;

/// <summary>
/// Manages PublicAPI analysis and status promotion (Unshipped to Shipped).
/// </summary>
static class PublicApiManager
{
    const string ShippedApiFileName = "PublicAPI.Shipped.txt";
    const string UnshippedApiFileName = "PublicAPI.Unshipped.txt";
    const string NullableEnableHeader = "#nullable enable";

    public static VersionBumpType DetermineHybridBumpType(AbsolutePath rootDirectory, Project project, string? baselineTag)
    {
        var unshippedPath = project.Directory / UnshippedApiFileName;

        if (unshippedPath.FileExists())
        {
            var lines = File.ReadAllLines(unshippedPath);
            var hasRemoved = false;
            var hasAdded = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.AsSpan().Trim();

                if (line.IsEmpty || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("REMOVED", StringComparison.Ordinal))
                {
                    hasRemoved = true;
                    break;
                }

                hasAdded = true;
            }

            if (hasRemoved)
            {
                return VersionBumpType.Major;
            }

            if (hasAdded)
            {
                return VersionBumpType.Minor;
            }
        }

        var shippedPath = project.Directory / ShippedApiFileName;

        if (shippedPath.FileExists() && !string.IsNullOrEmpty(baselineTag))
        {
            var diff = GitTasksHelper.GitDiff(rootDirectory, rootDirectory.GetRelativePathTo(shippedPath), baselineTag);

            var hasAdditions = false;

            foreach (var output in diff)
            {
                var text = output.Text.AsSpan();

                if (text.StartsWith("---", StringComparison.Ordinal)
                    || text.StartsWith("+++", StringComparison.Ordinal)
                    || text.StartsWith("@@", StringComparison.Ordinal))
                {
                    continue;
                }

                if (text.StartsWith("-", StringComparison.Ordinal))
                {
                    return VersionBumpType.Major;
                }

                if (text.StartsWith("+", StringComparison.Ordinal))
                {
                    hasAdditions = true;
                }
            }

            if (hasAdditions)
            {
                return VersionBumpType.Minor;
            }
        }

        return VersionBumpType.Patch;
    }

    public static void PromoteUnshippedApiToShipped(Project project, ILogger logger)
    {
        var unshippedPath = project.Directory / UnshippedApiFileName;
        var shippedPath = project.Directory / ShippedApiFileName;

        if (!unshippedPath.FileExists())
        {
            return;
        }

        List<string> unshippedLines = [.. File.ReadAllLines(unshippedPath).Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))];

        if (unshippedLines.Count == 0)
        {
            return;
        }

        HashSet<string> shippedLines = shippedPath.FileExists()
            ? [.. File.ReadAllLines(shippedPath).Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))]
            : [NullableEnableHeader];

        foreach (var line in unshippedLines)
        {
            if (line.StartsWith("REMOVED", StringComparison.Ordinal))
            {
                var apiToRemove = line["REMOVED".Length..].Trim();
                _ = shippedLines.Remove(apiToRemove);
            }
            else
            {
                _ = shippedLines.Add(line);
            }
        }

        List<string> sortedShipped =
        [
            .. shippedLines
                .Where(l => !string.Equals(l, NullableEnableHeader, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
        ];

        sortedShipped.Insert(0, NullableEnableHeader);

        File.WriteAllLines(shippedPath, sortedShipped);
        File.WriteAllLines(unshippedPath, [NullableEnableHeader]);

        logger.PromotedPublicApi(project.Name);
    }
}
