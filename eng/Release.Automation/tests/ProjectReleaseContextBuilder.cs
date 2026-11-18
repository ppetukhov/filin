using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation.Tests;

/// <summary>
/// Fluent builder for constructing <see cref="ProjectReleaseContext"/> instances in unit tests.
/// </summary>
sealed class ProjectReleaseContextBuilder
{
    AbsolutePath _rootDirectory = null!;
    Project _project = null!;
    string _relativePath = "src/App";
    IReadOnlyList<Project> _dependencies = [];
    bool _isNewProject;
    string _currentVersion = "1.0.0";
    string? _baselineTag;
    VersionBumpType _bumpType = VersionBumpType.None;
    ReleaseChannel _channel = ReleaseChannel.Stable;
    bool _promoteToMajor;

    public static ProjectReleaseContextBuilder Create(Project project, AbsolutePath rootDirectory) =>
        new ProjectReleaseContextBuilder()
            .ForProject(project)
            .WithRootDirectory(rootDirectory);

    public ProjectReleaseContextBuilder ForProject(Project project)
    {
        _project = project;
        return this;
    }

    public ProjectReleaseContextBuilder WithRootDirectory(AbsolutePath rootDirectory)
    {
        _rootDirectory = rootDirectory;
        return this;
    }

    public ProjectReleaseContextBuilder WithRelativePath(string relativePath)
    {
        _relativePath = relativePath;
        return this;
    }

    public ProjectReleaseContextBuilder WithDependencies(params Project[] dependencies)
    {
        _dependencies = dependencies;
        return this;
    }

    public ProjectReleaseContextBuilder WithDependencies(IEnumerable<Project> dependencies)
    {
        _dependencies = [.. dependencies];
        return this;
    }

    public ProjectReleaseContextBuilder IsNewProject(bool isNew = true)
    {
        _isNewProject = isNew;
        return this;
    }

    public ProjectReleaseContextBuilder WithCurrentVersion(string version)
    {
        _currentVersion = version;
        return this;
    }

    public ProjectReleaseContextBuilder WithBaselineTag(string? tag)
    {
        _baselineTag = tag;
        return this;
    }

    public ProjectReleaseContextBuilder WithBumpType(VersionBumpType bumpType)
    {
        _bumpType = bumpType;
        return this;
    }

    public ProjectReleaseContextBuilder WithChannel(ReleaseChannel channel)
    {
        _channel = channel;
        return this;
    }

    public ProjectReleaseContextBuilder WithPromoteToMajor(bool promote = true)
    {
        _promoteToMajor = promote;
        return this;
    }

    public ProjectReleaseContext Build()
    {
        return new ProjectReleaseContext
        {
            RootDirectory = _rootDirectory,
            Project = _project,
            ReleativePath = _relativePath,
            Dependencies = [.. _dependencies],
            IsNewProject = _isNewProject,
            CurrentVersion = _currentVersion,
            BaselineTag = _baselineTag,
            BumpType = _bumpType,
            Channel = _channel,
            PromoteToMajor = _promoteToMajor
        };
    }

    public static implicit operator ProjectReleaseContext(ProjectReleaseContextBuilder builder) => builder.Build();
}
