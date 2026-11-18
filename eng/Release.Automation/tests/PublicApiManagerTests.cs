using Microsoft.Extensions.Logging.Abstractions;

namespace Release.Automation.Tests;

/// <summary>
/// Contains unit and integration tests for <see cref="PublicApiManager"/> verifying PublicAPI analysis and promotion.
/// </summary>
public class PublicApiManagerTests
{
    static readonly NullLogger Logger = NullLogger.Instance;

    #region DetermineHybridBumpType Tests

    [Fact]
    public async Task DetermineHybridBumpType_ShouldReturnMajor_WhenUnshippedContainsRemovedMarker()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var unshippedContent = """
            #nullable enable
            REMOVED PublicAPI.MyClass.OldMethod() -> void
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, baselineTag: null);

        // Assert
        Assert.Equal(VersionBumpType.Major, bumpType);
    }

    [Fact]
    public async Task DetermineHybridBumpType_ShouldReturnMinor_WhenUnshippedContainsNewSignatures()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var unshippedContent = """
            #nullable enable
            PublicAPI.MyClass.NewMethod() -> void
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, baselineTag: null);

        // Assert
        Assert.Equal(VersionBumpType.Minor, bumpType);
    }

    [Fact]
    public async Task DetermineHybridBumpType_ShouldIgnoreCommentsAndNullableHeaderInUnshipped()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var unshippedContent = """
            #nullable enable
            # This is a comment explaining API changes
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, baselineTag: null);

        // Assert
        Assert.Equal(VersionBumpType.Patch, bumpType);
    }

    [Fact]
    public async Task DetermineHybridBumpType_ShouldReturnMajor_WhenShippedHasDeletedLinesInGitDiff()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var initialShipped = """
            #nullable enable
            PublicAPI.MyClass.Method1() -> void
            PublicAPI.MyClass.Method2() -> void
            """;
        await ws.CreateFileCommitWithTagsAsync("src/CoreLib/PublicAPI.Shipped.txt", initialShipped, "CoreLib-v1.0.0");

        var updatedShipped = """
            #nullable enable
            PublicAPI.MyClass.Method1() -> void
            """;
        await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Shipped.txt", updatedShipped);
        await ws.CommitAllAsync("remove breaking method from shipped API");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, "CoreLib-v1.0.0");

        // Assert
        Assert.Equal(VersionBumpType.Major, bumpType);
    }

    [Fact]
    public async Task DetermineHybridBumpType_ShouldReturnMinor_WhenShippedHasAddedLinesInGitDiff()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var initialShipped = """
            #nullable enable
            PublicAPI.MyClass.Method1() -> void
            """;
        await ws.CreateFileCommitWithTagsAsync("src/CoreLib/PublicAPI.Shipped.txt", initialShipped, "CoreLib-v1.0.0");

        var updatedShipped = """
            #nullable enable
            PublicAPI.MyClass.Method1() -> void
            PublicAPI.MyClass.Method2() -> void
            """;
        await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", updatedShipped);
        await ws.CommitAllAsync("add new method to shipped API");

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, "CoreLib-v1.0.0");

        // Assert
        Assert.Equal(VersionBumpType.Minor, bumpType);
    }

    [Fact]
    public async Task DetermineHybridBumpType_ShouldReturnPatch_WhenNoUnshippedOrShippedChangesExist()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        var bumpType = PublicApiManager.DetermineHybridBumpType(ws.RootDirectory, project, baselineTag: null);

        // Assert
        Assert.Equal(VersionBumpType.Patch, bumpType);
    }

    #endregion

    #region PromoteUnshippedApiToShipped Tests

    [Fact]
    public async Task PromoteUnshippedApiToShipped_ShouldDoNothing_WhenUnshippedFileDoesNotExist()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        var shippedPath = project.Directory / "PublicAPI.Shipped.txt";

        // Act
        PublicApiManager.PromoteUnshippedApiToShipped(project, Logger);

        // Assert
        Assert.False(File.Exists(shippedPath));
    }

    [Fact]
    public async Task PromoteUnshippedApiToShipped_ShouldDoNothing_WhenUnshippedContainsOnlyHeaderAndComments()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var unshippedContent = """
            #nullable enable
            # No actual API additions here
            """;
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        var shippedPath = project.Directory / "PublicAPI.Shipped.txt";

        // Act
        PublicApiManager.PromoteUnshippedApiToShipped(project, Logger);

        // Assert
        Assert.False(File.Exists(shippedPath));
    }

    [Fact]
    public async Task PromoteUnshippedApiToShipped_ShouldMergeUnshippedIntoShipped_AndResetUnshippedFile()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var shippedContent = """
            #nullable enable
            CoreLib.ClassA.MethodA() -> void
            """;
        var unshippedContent = """
            #nullable enable
            CoreLib.ClassB.MethodB() -> void
            """;

        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Shipped.txt", shippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        PublicApiManager.PromoteUnshippedApiToShipped(project, Logger);

        // Assert Shipped file updated with merged entries
        var shippedLines = await File.ReadAllLinesAsync(project.Directory / "PublicAPI.Shipped.txt");
        Assert.Equal("#nullable enable", shippedLines[0]);
        Assert.Contains("CoreLib.ClassA.MethodA() -> void", shippedLines);
        Assert.Contains("CoreLib.ClassB.MethodB() -> void", shippedLines);

        // Assert Unshipped file reset to header only
        var unshippedLines = await File.ReadAllLinesAsync(project.Directory / "PublicAPI.Unshipped.txt");
        Assert.Single(unshippedLines);
        Assert.Equal("#nullable enable", unshippedLines[0]);
    }

    [Fact]
    public async Task PromoteUnshippedApiToShipped_ShouldRemoveSignatures_WhenUnshippedContainsRemovedMarker()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var shippedContent = """
            #nullable enable
            CoreLib.ClassA.KeepMethod() -> void
            CoreLib.ClassA.RemoveMethod() -> void
            """;
        var unshippedContent = """
            #nullable enable
            REMOVED CoreLib.ClassA.RemoveMethod() -> void
            CoreLib.ClassB.NewMethod() -> void
            """;

        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Shipped.txt", shippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        PublicApiManager.PromoteUnshippedApiToShipped(project, Logger);

        // Assert
        var shippedLines = await File.ReadAllLinesAsync(project.Directory / "PublicAPI.Shipped.txt");
        Assert.Contains("CoreLib.ClassA.KeepMethod() -> void", shippedLines);
        Assert.Contains("CoreLib.ClassB.NewMethod() -> void", shippedLines);
        Assert.DoesNotContain("CoreLib.ClassA.RemoveMethod() -> void", shippedLines);
    }

    [Fact]
    public async Task PromoteUnshippedApiToShipped_ShouldSortEntriesAlphabetically_WithHeaderAtTop()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        var unshippedContent = """
            #nullable enable
            ZClass.Method() -> void
            AClass.Method() -> void
            MClass.Method() -> void
            """;

        _ = await ws.CreateTextFileAsync("src/CoreLib/PublicAPI.Unshipped.txt", unshippedContent);
        _ = await ws.CreateTextFileAsync("src/CoreLib/CoreLib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var solution = await ws.CreateSolutionAsync([("CoreLib", "src/CoreLib/CoreLib.csproj")]);
        var project = solution.AllProjects.First(p => p.Name == "CoreLib");

        // Act
        PublicApiManager.PromoteUnshippedApiToShipped(project, Logger);

        // Assert
        var shippedLines = await File.ReadAllLinesAsync(project.Directory / "PublicAPI.Shipped.txt");
        Assert.Equal(4, shippedLines.Length);
        Assert.Equal("#nullable enable", shippedLines[0]);
        Assert.Equal("AClass.Method() -> void", shippedLines[1]);
        Assert.Equal("MClass.Method() -> void", shippedLines[2]);
        Assert.Equal("ZClass.Method() -> void", shippedLines[3]);
    }

    #endregion
}
