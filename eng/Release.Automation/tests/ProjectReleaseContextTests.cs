namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for <see cref="ProjectReleaseContext"/> verifying property mutation and version cache invalidation.
/// </summary>
public class ProjectReleaseContextTests
{
    [Fact]
    public async Task CalculatedVersion_ShouldResetCache_WhenBumpTypeOrHasSecurityFixChanges()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        var ctx = ProjectReleaseContextBuilder.Create(project, ws.RootDirectory)
            .WithCurrentVersion("1.0.0")
            .WithBumpType(VersionBumpType.Patch)
            .WithChannel(ReleaseChannel.Stable)
            .Build();

        // Act 1: Initial calculation (1.0.0 + Patch = 1.0.1)
        var initialVersion = ctx.CalculatedVersion;
        Assert.Equal("1.0.1", initialVersion);

        // Act 2: Mutate BumpType to Minor (1.0.0 + Minor = 1.1.0)
        ctx.BumpType = VersionBumpType.Minor;
        var versionAfterBumpTypeChange = ctx.CalculatedVersion;

        // Act 3: Mutate HasSecurityFix (triggers cache reset)
        ctx.HasSecurityFix = true;
        var versionAfterSecurityFixChange = ctx.CalculatedVersion;

        // Assert
        Assert.Equal("1.1.0", versionAfterBumpTypeChange);
        Assert.Equal("1.1.0", versionAfterSecurityFixChange);
    }

    [Fact]
    public async Task CalculatedVersion_ShouldNotResetCache_WhenMutatedWithSameValue()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        var ctx = ProjectReleaseContextBuilder.Create(project, ws.RootDirectory)
            .WithCurrentVersion("1.0.0")
            .WithBumpType(VersionBumpType.Minor)
            .WithChannel(ReleaseChannel.Stable)
            .Build();

        var firstAccess = ctx.CalculatedVersion;

        // Act: Assign identical value to BumpType
        ctx.BumpType = VersionBumpType.Minor;
        var secondAccess = ctx.CalculatedVersion;

        // Assert: Value remains identical and cached
        Assert.Equal("1.1.0", firstAccess);
        Assert.Equal("1.1.0", secondAccess);
    }
}
