using System.Collections.Immutable;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation;

sealed class ProjectReleaseContext
{
    public required AbsolutePath RootDirectory { get; init; }
    public required Project Project { get; init; }
    public required string ReleativePath { get; init; }
    public required ImmutableList<Project> Dependencies { get; init; }
    public required bool IsNewProject { get; init; }
    public required string CurrentVersion { get; init; }
    public string? BaselineTag { get; init; }
    public VersionBumpType BumpType
    {
        get;
        set
        {
            if (value != field)
            {
                CalculatedVersion = null!;
            }
            field = value;
        }
    }
    public bool HasSecurityFix
    {
        get;
        set
        {
            if (value != field)
            {
                CalculatedVersion = null!;
            }
            field = value;
        }
    }
    public required ReleaseChannel Channel { get; init; }
    public required bool PromoteToMajor { get; init; }
    public string CalculatedVersion
    {
        get
        {
            field ??= VersionCalculator.CalculateNextVersion(
                RootDirectory,
                CurrentVersion,
                BumpType,
                Channel,
                IsNewProject,
                PromoteToMajor);

            return field;
        }

        private set;
    }
}
