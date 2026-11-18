using System.Collections.Immutable;

using Nuke.Common.ProjectModel;

namespace Release.Automation;

/// <summary>
/// Provides utility methods for constructing and grouping project dependency graphs in topological order.
/// </summary>
static class DependencyGraphBuilder
{
    /// <summary>
    /// Groups solution projects into topological dependency levels starting with projects that have no direct dependencies (Level 0).
    /// </summary>
    public static ImmutableList<ImmutableList<ProjectReleaseContext>> BuildDependencyLevels(
        ImmutableDictionary<Project, ProjectReleaseContext> contexts)
    {
        var levelsBuilder = ImmutableList<ImmutableList<ProjectReleaseContext>>.Empty.ToBuilder();
        var processedProjects = new HashSet<Project>();
        var remainingContexts = new HashSet<ProjectReleaseContext>(contexts.Values);

        while (remainingContexts.Count > 0)
        {
            // Select projects whose dependencies have all been processed already (or have no dependencies)
            var currentLevel = remainingContexts
                .Where(ctx => ctx.Dependencies.All(dep => processedProjects.Contains(dep)))
                .ToImmutableList();

            if (currentLevel.IsEmpty)
            {
                throw new InvalidOperationException("Circular dependency detected or referenced project is missing from context dictionary.");
            }

            levelsBuilder.Add(currentLevel);

            // Mark current level projects as processed
            foreach (var ctx in currentLevel)
            {
                _ = processedProjects.Add(ctx.Project);
                _ = remainingContexts.Remove(ctx);
            }
        }

        return levelsBuilder.ToImmutable();
    }
}
