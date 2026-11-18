using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Build.Construction;
using Microsoft.Extensions.Logging;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation;

/// <summary>
/// Encapsulates the multi-pass version bumping pipeline for solution projects.
/// </summary>
static partial class VersionBumpProcessor
{
    const string IsPackableProperty = "IsPackable";
    const string GeneratePackageOnBuildProperty = "GeneratePackageOnBuild";
    const string TrueValue = "true";
    const string DirectoryPackagesPropsFileName = "Directory.Packages.props";

    [GeneratedRegex(@"^[+-]\s*<PackageVersion\s+Include=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    private static partial Regex PackageVersionDiffRegex();

    /// <summary>
    /// Executes the full version bump evaluation pipeline (Passes 1–4) and updates central versions.
    /// </summary>
    public static ImmutableList<ProjectReleaseContext> Execute(
        ImmutableList<Project> allSolutionProjects,
        AbsolutePath rootDirectory,
        RelativePath versionsPropsFile,
        ReleaseChannel channel,
        bool promoteToMajor,
        IReadOnlyCollection<string> forceProjects,
        ILogger logger)
    {
        var versionsPropsPath = rootDirectory / versionsPropsFile;

        var versionsPropsDocument = VersionsPropsDocument.LoadOrCreate(versionsPropsPath, logger);

        // Pass 1: Evaluate direct commits, CPM updates, and PublicAPI status
        var contexts = EvaluateDirectChanges(rootDirectory, allSolutionProjects, versionsPropsDocument, channel, promoteToMajor, logger);

        // Pass 2: Cascade bumps for Major changes or Security fixes in dependencies
        EvaluateCascadeBumps(contexts, logger);

        // Pass 3: Evaluate --force-projects flag
        EvaluateForceProjects(contexts, versionsPropsFile, forceProjects, logger);

        // Pass 4: Calculate final versions, update XML nodes, and promote PublicAPI
        var activeProjects = ApplyFinalVersionBumps(contexts, versionsPropsDocument, channel, logger);

        // Save updated XML file if explicit release had changes
        if (channel.IsExplicitRelease && versionsPropsDocument.Changed)
        {
            versionsPropsDocument.Save(versionsPropsPath, logger);
        }

        return activeProjects;
    }

    static ImmutableDictionary<Project, ProjectReleaseContext> EvaluateDirectChanges(
        AbsolutePath rootDirectory,
        ImmutableList<Project> allSolutionProjects,
        VersionsPropsDocument versionsPropsDocument,
        ReleaseChannel channel,
        bool promoteToMajor,
        ILogger logger)
    {
        var contexts = ImmutableDictionary<Project, ProjectReleaseContext>.Empty.ToBuilder();

        var allPackableProjects = allSolutionProjects
            .Where(p => string.Equals(p.GetProperty(IsPackableProperty), TrueValue, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(p.GetProperty(GeneratePackageOnBuildProperty), TrueValue, StringComparison.OrdinalIgnoreCase));

        foreach (var project in allPackableProjects)
        {
            var dependencies = GetDirectProjectDependencies(project, allSolutionProjects);

            var isNewProject = false;

            if (!versionsPropsDocument.TryGetVersion(project.Name, out var version))
            {
                version = promoteToMajor && channel.IsExplicitRelease ? "1.0.0" : "0.1.0";
                isNewProject = true;
            }

            var tag = $"{project.Name}-v{version}";
            var baselineTag = GitTasksHelper.DoesGitTagExist(rootDirectory, tag) ? tag : null;

            var projectDirectory = (RelativePath)rootDirectory.GetRelativePathTo(project.Directory).ToString().Replace('\\', '/');

            var hasDirectCommits = GitTasksHelper.HasCommitsSince(rootDirectory, projectDirectory, baselineTag)
                                || HasCentralPackageChanges(rootDirectory, project, baselineTag, logger);

            var hasSecurityFix = hasDirectCommits && GitTasksHelper.HasSecurityFixMarker(rootDirectory, projectDirectory, baselineTag);

            var bumpType = VersionBumpType.None;

            if (hasDirectCommits)
            {
                if (GitTasksHelper.HasBreakingCommitMarker(rootDirectory, projectDirectory, baselineTag))
                {
                    logger.BehavioralBreakingChangeDetected(project.Name);
                    bumpType = VersionBumpType.Major;
                }
                else
                {
                    var hybridBump = PublicApiManager.DetermineHybridBumpType(rootDirectory, project, baselineTag);

                    bumpType = hybridBump switch
                    {
                        VersionBumpType.Major => VersionBumpType.Major,
                        VersionBumpType.Minor => VersionBumpType.Minor,
                        VersionBumpType.Patch or VersionBumpType.None or
                        _ => GitTasksHelper.HasFeatureCommitMarker(rootDirectory, projectDirectory, baselineTag)
                                ? VersionBumpType.Minor
                                : VersionBumpType.Patch,
                    };
                }
            }

            contexts[project] = new ProjectReleaseContext
            {
                RootDirectory = rootDirectory,
                Project = project,
                ReleativePath = projectDirectory,
                Dependencies = dependencies,
                IsNewProject = isNewProject,
                CurrentVersion = version,
                BaselineTag = baselineTag,
                BumpType = bumpType,
                Channel = channel,
                PromoteToMajor = promoteToMajor,
                HasSecurityFix = hasSecurityFix
            };
        }

        return contexts.ToImmutable();
    }

    static void EvaluateCascadeBumps(ImmutableDictionary<Project, ProjectReleaseContext> contexts, ILogger logger)
    {
        var levels = DependencyGraphBuilder.BuildDependencyLevels(contexts);

        // Skip Level 0 (base projects with no dependencies)
        foreach (var level in levels.Skip(1))
        {
            foreach (var ctx in level)
            {
                if (ctx.BumpType != VersionBumpType.None)
                {
                    continue;
                }

                // Check if any direct dependency was bumped or marked with a security fix
                var needsCascade = ctx.Dependencies.Any(dep =>
                    contexts.TryGetValue(dep, out var depCtx)
                    && (depCtx.BumpType == VersionBumpType.Major || depCtx.HasSecurityFix));

                if (needsCascade)
                {
                    ctx.BumpType = VersionBumpType.Patch;
                    ctx.HasSecurityFix = true;
                    logger.CascadeDependencyBump(ctx.Project.Name);
                }
            }
        }
    }

    static void EvaluateForceProjects(
        ImmutableDictionary<Project, ProjectReleaseContext> contexts,
        RelativePath versionsPropsFile,
        IReadOnlyCollection<string> forceProjects,
        ILogger logger)
    {
        foreach (var ctx in contexts.Values)
        {
            if (ctx.BumpType != VersionBumpType.None)
            {
                continue;
            }

            if (!forceProjects.Contains(ctx.Project.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(versionsPropsFile, ctx, contexts))
            {
                ctx.BumpType = VersionBumpType.Patch;
                logger.ForcedReleaseAccepted(ctx.Project.Name);
            }
            else
            {
                logger.IdleForcedReleaseSkipped(ctx.Project.Name);
            }
        }
    }

    static ImmutableList<ProjectReleaseContext> ApplyFinalVersionBumps(
        ImmutableDictionary<Project, ProjectReleaseContext> contexts,
        VersionsPropsDocument versionsPropsDocument,
        ReleaseChannel channel,
        ILogger logger)
    {
        var activeProjects = ImmutableList<ProjectReleaseContext>.Empty.ToBuilder();

        foreach (var ctx in contexts.Values)
        {
            if (ctx.BumpType == VersionBumpType.None)
            {
                logger.ProjectSkippedNoChanges(ctx.Project.Name);
                continue;
            }

            if (ctx.IsNewProject)
            {
                logger.NewProjectDetected(ctx.Project.Name, ctx.CurrentVersion);

                versionsPropsDocument[ctx.Project.Name] = ctx.CurrentVersion;
            }

            activeProjects.Add(ctx);

            if (ctx.Channel.IsExplicitRelease)
            {
                if (versionsPropsDocument.TrySetVersion(ctx.Project.Name, ctx.CalculatedVersion))
                {
#pragma warning disable CA1873 // Avoid potentially expensive logging
                    logger.ReleaseBump(ctx.Channel.ToString(), ctx.Project.Name, ctx.CurrentVersion, ctx.CalculatedVersion, ctx.BumpType.ToString());
#pragma warning restore CA1873 // Avoid potentially expensive logging
                }

                if (channel == ReleaseChannel.Stable)
                {
                    PublicApiManager.PromoteUnshippedApiToShipped(ctx.Project, logger);
                }
            }
        }

        return activeProjects.ToImmutable();
    }

    /// <summary>
    /// Retrieves direct project dependencies (ProjectReference) contained within the solution.
    /// </summary>
    static ImmutableList<Project> GetDirectProjectDependencies(Project project, ImmutableList<Project> allSolutionProjects)
    {
        var root = ProjectRootElement.Open(project.Path);

        var projectReferences = root.Items
            .Where(item => string.Equals(item.ItemType, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Include)
            .Where(include => !string.IsNullOrEmpty(include));

        var result = ImmutableList<Project>.Empty.ToBuilder();

        foreach (var pref in projectReferences)
        {
            var prefPath = project.Directory / pref;

            foreach (var proj in allSolutionProjects)
            {
                if (prefPath == proj.Path)
                {
                    if (!result.Contains(proj))
                    {
                        result.Add(proj);
                    }
                    break;
                }
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Checks if any packages referenced by the current project had their versions 
    /// updated in Directory.Packages.props since the specified baseline tag.
    /// </summary>
    static bool HasCentralPackageChanges(
        AbsolutePath rootDirectory,
        Project project,
        string? baselineTag,
        ILogger logger)
    {
        // 1. Fetch git diff for Directory.Packages.props
        var diffOutput = GitTasksHelper.GitDiff(rootDirectory, (RelativePath)DirectoryPackagesPropsFileName, baselineTag);

        if (diffOutput.Count == 0)
        {
            return false;
        }

        var diffText = string.Join("\n", diffOutput.Select(r => r.Text));

        if (string.IsNullOrWhiteSpace(diffText))
        {
            return false;
        }

        // 2. Extract package names from added/modified lines (+ or - PackageVersion)
        var changedPackages = GetChangedPackageNames(diffText);

        if (changedPackages.IsEmpty)
        {
            return false;
        }

        var hasChanges = false;

        foreach (var packageName in changedPackages)
        {
            if (project.HasPackageReference(packageName))
            {
                logger.CentralPackageBump(project.Name, packageName);
                hasChanges = true;
            }
        }

        return hasChanges;
    }

    /// <summary>
    /// Parses git diff output and returns a distinct set of modified package names.
    /// </summary>
    internal static ImmutableHashSet<string> GetChangedPackageNames(string diffText)
    {
        var matches = PackageVersionDiffRegex().Matches(diffText);

        if (matches.Count == 0)
        {
            return [];
        }

        var changedPackages = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal).ToBuilder();

        foreach (Match match in matches)
        {
            // Access Group 1 to extract the package name inside Include="..."
            if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                _ = changedPackages.Add(match.Groups[1].Value);
            }
        }

        return changedPackages.ToImmutable();
    }
}
