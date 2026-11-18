using System.Collections.Immutable;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for evaluating project dependency updates using <see cref="DependencyReleaseEvaluator"/>.
/// </summary>
public class DependencyReleaseEvaluatorTests
{
    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnTrue_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var appPath = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("MainApp", "src/App/MainApp.csproj")]);
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var ctx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .IsNewProject()
            .WithBaselineTag(null);

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [appProject] = ctx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: ctx,
            contexts: contexts);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnFalse_WhenNoDependenciesExist()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var standalonePath = await ws.CreateTextFileAsync("src/Standalone/StandaloneApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("StandaloneApp", "src/Standalone/StandaloneApp.csproj")]);
        var standaloneProject = solution.AllProjects.First(p => p.Name == "StandaloneApp");

        var ctx = ProjectReleaseContextBuilder.Create(standaloneProject, ws.RootDirectory)
            .WithBaselineTag("StandaloneApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [standaloneProject] = ctx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: ctx,
            contexts: contexts);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnFalse_WhenDependencyMissingFromContextsDictionary()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync(
            [("CoreLib", "src/CoreLib/CoreLib.csproj"), ("MainApp", "src/App/MainApp.csproj")]
        );

        var depProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(depProject)
            .WithBaselineTag("MainApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(VersionBumpType.None, ReleaseChannel.Stable, false)]
    [InlineData(VersionBumpType.Minor, ReleaseChannel.Stable, true)]
    [InlineData(VersionBumpType.Major, ReleaseChannel.Stable, true)]
    [InlineData(VersionBumpType.Patch, ReleaseChannel.Stable, true)]
    [InlineData(VersionBumpType.Minor, ReleaseChannel.Alpha, false)]
    internal async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldEvaluateCorrectly_ForVariousBumpAndChannelCombinations(
        VersionBumpType bumpType,
        ReleaseChannel channel,
        bool expectedResult)
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync(
            [("CoreLib", "src/CoreLib/CoreLib.csproj"), ("MainApp", "src/App/MainApp.csproj")]
        );

        var depProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var depCtx = ProjectReleaseContextBuilder.Create(depProject, ws.RootDirectory)
            .WithBaselineTag("CoreLib-v1.0.0")
            .WithBumpType(bumpType)
            .WithChannel(channel);

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(depProject)
            .WithBaselineTag("MainApp-v1.0.0")
            .WithChannel(channel);

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [depProject] = depCtx,
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #region Edge Cases Tests for DependencyReleaseEvaluator

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnTrue_WhenHistoricalVersionsPropsAtTagIsInvalid()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Commit invalid XML in Versions.props at baseline tag
        var invalidXml = "<Project><PropertyGroup>Corrupted XML Content";
        await ws.CreateFileCommitWithTagsAsync("Versions.props", invalidXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync(
            [("CoreLib", "src/CoreLib/CoreLib.csproj"), ("MainApp", "src/App/MainApp.csproj")]
        );

        var depProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var depCtx = ProjectReleaseContextBuilder.Create(depProject, ws.RootDirectory)
            .WithCurrentVersion("1.0.0");

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(depProject)
            .WithBaselineTag("MainApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [depProject] = depCtx,
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert: Failure to parse historical XML forces LoadVersionsDocAtTag to return null, triggering true
        Assert.True(result);
    }

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnTrue_WhenDependencyIsMissingFromHistoricalVersionsProps()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Baseline XML contains App version but lacks CoreLibVersion
        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <MainAppVersion>1.0.0</MainAppVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync(
            [("CoreLib", "src/CoreLib/CoreLib.csproj"), ("MainApp", "src/App/MainApp.csproj")]
        );

        var depProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var depCtx = ProjectReleaseContextBuilder.Create(depProject, ws.RootDirectory)
            .WithCurrentVersion("1.0.0");

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(depProject)
            .WithBaselineTag("MainApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [depProject] = depCtx,
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert: Missing dependency node in historical XML triggers true
        Assert.True(result);
    }

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnFalse_WhenHistoricalVersionMatchesEffectiveVersion()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <MainAppVersion>1.0.0</MainAppVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync(
            [("CoreLib", "src/CoreLib/CoreLib.csproj"), ("MainApp", "src/App/MainApp.csproj")]
        );

        var depProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        var depCtx = ProjectReleaseContextBuilder.Create(depProject, ws.RootDirectory)
            .WithCurrentVersion("1.0.0")
            .WithBumpType(VersionBumpType.None);

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(depProject)
            .WithBaselineTag("MainApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [depProject] = depCtx,
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert: Versions are identical, no updates detected
        Assert.False(result);
    }

    [Fact]
    public async Task HaveAnyDependenciesUpdatedSinceBaseline_ShouldReturnTrue_WhenOneOfMultipleDependenciesIsUpdated()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var baselineVersionsXml = """
            <Project>
              <PropertyGroup>
                <CoreLibVersion>1.0.0</CoreLibVersion>
                <UtilLibVersion>1.0.0</UtilLibVersion>
                <MainAppVersion>1.0.0</MainAppVersion>
              </PropertyGroup>
            </Project>
            """;
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineVersionsXml, "MainApp-v1.0.0");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/UtilLib/UtilLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/App/MainApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([
            ("CoreLib", "src/CoreLib/CoreLib.csproj"),
            ("UtilLib", "src/UtilLib/UtilLib.csproj"),
            ("MainApp", "src/App/MainApp.csproj")
        ]);

        var coreProject = solution.AllProjects.First(p => p.Name == "CoreLib");
        var utilProject = solution.AllProjects.First(p => p.Name == "UtilLib");
        var appProject = solution.AllProjects.First(p => p.Name == "MainApp");

        // CoreLib remains at 1.0.0
        var coreCtx = ProjectReleaseContextBuilder.Create(coreProject, ws.RootDirectory)
            .WithCurrentVersion("1.0.0")
            .WithBumpType(VersionBumpType.None);

        // UtilLib gets a Minor bump (1.0.0 -> 1.1.0)
        var utilCtx = ProjectReleaseContextBuilder.Create(utilProject, ws.RootDirectory)
            .WithCurrentVersion("1.0.0")
            .WithBumpType(VersionBumpType.Minor)
            .WithChannel(ReleaseChannel.Stable);

        var appCtx = ProjectReleaseContextBuilder.Create(appProject, ws.RootDirectory)
            .WithDependencies(coreProject, utilProject)
            .WithBaselineTag("MainApp-v1.0.0");

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [coreProject] = coreCtx,
            [utilProject] = utilCtx,
            [appProject] = appCtx
        }.ToImmutableDictionary();

        // Act
        var result = DependencyReleaseEvaluator.HaveAnyDependenciesUpdatedSinceBaseline(
            versionsPropsFile: (RelativePath)"Versions.props",
            ctx: appCtx,
            contexts: contexts);

        // Assert: Updating 1 out of 2 dependencies triggers true
        Assert.True(result);
    }

    #endregion
}
