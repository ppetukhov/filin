using System.Collections.Immutable;

using Nuke.Common.ProjectModel;

namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for <see cref="DependencyGraphBuilder"/> verifying topological sorting and level generation.
/// </summary>
public class DependencyGraphBuilderTests
{
    [Fact]
    public async Task BuildDependencyLevels_ShouldGroupIndependentProjects_IntoFirstLevel()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        _ = await ws.CreateTextFileAsync("src/LibA/LibA.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/LibB/LibB.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([
            ("LibA", "src/LibA/LibA.csproj"),
            ("LibB", "src/LibB/LibB.csproj")
        ]);

        var libA = solution.AllProjects.First(p => p.Name == "LibA");
        var libB = solution.AllProjects.First(p => p.Name == "LibB");

        var ctxA = ProjectReleaseContextBuilder.Create(libA, ws.RootDirectory).Build();
        var ctxB = ProjectReleaseContextBuilder.Create(libB, ws.RootDirectory).Build();

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [libA] = ctxA,
            [libB] = ctxB
        }.ToImmutableDictionary();

        // Act
        var levels = DependencyGraphBuilder.BuildDependencyLevels(contexts);

        // Assert: Both independent projects belong to Level 0
        var level0 = Assert.Single(levels);
        Assert.Equal(2, level0.Count);
        Assert.Contains(ctxA, level0);
        Assert.Contains(ctxB, level0);
    }

    [Fact]
    public async Task BuildDependencyLevels_ShouldOrderLinearDependencyChain_IntoSequentialLevels()
    {
        // Arrange (Chain: LibA <- LibB <- AppHost)
        using var ws = TestWorkspace.CreateWorkspace();
        _ = await ws.CreateTextFileAsync("src/LibA/LibA.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/LibB/LibB.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/AppHost/AppHost.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([
            ("LibA", "src/LibA/LibA.csproj"),
            ("LibB", "src/LibB/LibB.csproj"),
            ("AppHost", "src/AppHost/AppHost.csproj")
        ]);

        var libA = solution.AllProjects.First(p => p.Name == "LibA");
        var libB = solution.AllProjects.First(p => p.Name == "LibB");
        var appHost = solution.AllProjects.First(p => p.Name == "AppHost");

        var ctxA = ProjectReleaseContextBuilder.Create(libA, ws.RootDirectory).Build();
        var ctxB = ProjectReleaseContextBuilder.Create(libB, ws.RootDirectory).WithDependencies(libA).Build();
        var ctxApp = ProjectReleaseContextBuilder.Create(appHost, ws.RootDirectory).WithDependencies(libB).Build();

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [libA] = ctxA,
            [libB] = ctxB,
            [appHost] = ctxApp
        }.ToImmutableDictionary();

        // Act
        var levels = DependencyGraphBuilder.BuildDependencyLevels(contexts);

        // Assert
        Assert.Equal(3, levels.Count);
        Assert.Single(levels[0], ctxA);
        Assert.Single(levels[1], ctxB);
        Assert.Single(levels[2], ctxApp);
    }

    [Fact]
    public async Task BuildDependencyLevels_ShouldGroupDiamondGraph_Correctly()
    {
        // Arrange (Diamond Graph: LibA <- [LibB, LibC] <- AppHost)
        using var ws = TestWorkspace.CreateWorkspace();
        _ = await ws.CreateTextFileAsync("src/LibA/LibA.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/LibB/LibB.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/LibC/LibC.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/AppHost/AppHost.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([
            ("LibA", "src/LibA/LibA.csproj"),
            ("LibB", "src/LibB/LibB.csproj"),
            ("LibC", "src/LibC/LibC.csproj"),
            ("AppHost", "src/AppHost/AppHost.csproj")
        ]);

        var libA = solution.AllProjects.First(p => p.Name == "LibA");
        var libB = solution.AllProjects.First(p => p.Name == "LibB");
        var libC = solution.AllProjects.First(p => p.Name == "LibC");
        var appHost = solution.AllProjects.First(p => p.Name == "AppHost");

        var ctxA = ProjectReleaseContextBuilder.Create(libA, ws.RootDirectory).Build();
        var ctxB = ProjectReleaseContextBuilder.Create(libB, ws.RootDirectory).WithDependencies(libA).Build();
        var ctxC = ProjectReleaseContextBuilder.Create(libC, ws.RootDirectory).WithDependencies(libA).Build();
        var ctxApp = ProjectReleaseContextBuilder.Create(appHost, ws.RootDirectory).WithDependencies(libB, libC).Build();

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [libA] = ctxA,
            [libB] = ctxB,
            [libC] = ctxC,
            [appHost] = ctxApp
        }.ToImmutableDictionary();

        // Act
        var levels = DependencyGraphBuilder.BuildDependencyLevels(contexts);

        // Assert
        Assert.Equal(3, levels.Count);
        Assert.Single(levels[0], ctxA);
        Assert.Equal(2, levels[1].Count);
        Assert.Contains(ctxB, levels[1]);
        Assert.Contains(ctxC, levels[1]);
        Assert.Single(levels[2], ctxApp);
    }

    [Fact]
    public async Task BuildDependencyLevels_ShouldThrowInvalidOperationException_WhenCircularDependencyExists()
    {
        // Arrange (Cycle: LibA -> LibB -> LibA)
        using var ws = TestWorkspace.CreateWorkspace();
        _ = await ws.CreateTextFileAsync("src/LibA/LibA.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        _ = await ws.CreateTextFileAsync("src/LibB/LibB.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([
            ("LibA", "src/LibA/LibA.csproj"),
            ("LibB", "src/LibB/LibB.csproj")
        ]);

        var libA = solution.AllProjects.First(p => p.Name == "LibA");
        var libB = solution.AllProjects.First(p => p.Name == "LibB");

        var ctxA = ProjectReleaseContextBuilder.Create(libA, ws.RootDirectory).WithDependencies(libB).Build();
        var ctxB = ProjectReleaseContextBuilder.Create(libB, ws.RootDirectory).WithDependencies(libA).Build();

        var contexts = new Dictionary<Project, ProjectReleaseContext>
        {
            [libA] = ctxA,
            [libB] = ctxB
        }.ToImmutableDictionary();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => DependencyGraphBuilder.BuildDependencyLevels(contexts));
        Assert.Contains("Circular dependency detected", ex.Message, StringComparison.Ordinal);
    }
}
