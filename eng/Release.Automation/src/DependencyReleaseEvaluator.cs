using System.Collections.Immutable;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation;

/// <summary>
/// Evaluates Git tags and verifies dependency updates across baseline releases.
/// </summary>
static class DependencyReleaseEvaluator
{
    public static bool HaveAnyDependenciesUpdatedSinceBaseline(
        RelativePath versionsPropsFile,
        ProjectReleaseContext ctx,
        ImmutableDictionary<Project, ProjectReleaseContext> contexts)
    {
        if (ctx.BaselineTag is null)
        {
            return true;
        }

        if (ctx.Dependencies.IsEmpty)
        {
            return false;
        }

        var historicalVersionProps = LoadVersionsDocAtTag(ctx.RootDirectory, versionsPropsFile, ctx.BaselineTag);

        if (historicalVersionProps is null)
        {
            return true;
        }

        foreach (var dep in ctx.Dependencies)
        {
            if (!contexts.TryGetValue(dep, out var depCtx))
            {
                continue;
            }

            var effectiveDepVersion = depCtx.BumpType != VersionBumpType.None && depCtx.Channel.IsExplicitRelease
                ? depCtx.CalculatedVersion
                : depCtx.CurrentVersion;

            if (!historicalVersionProps.TryGetVersion(depCtx.Project.Name, out var historicalDepVersion)
                || !string.Equals(historicalDepVersion, effectiveDepVersion, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    static VersionsPropsDocument? LoadVersionsDocAtTag(AbsolutePath rootDirectory, RelativePath versionsPropsFile, string tagName)
    {
        try
        {
            var output = GitTasksHelper.GitShow(rootDirectory, versionsPropsFile, tagName);

            var xmlContent = string.Join(Environment.NewLine, output.Select(o => o.Text));
            return VersionsPropsDocument.Parse(xmlContent);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            return null;
        }
    }
}
