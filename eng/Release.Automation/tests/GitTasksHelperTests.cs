using Nuke.Common.IO;

namespace Release.Automation.Tests;

/// <summary>
/// Contains comprehensive integration tests for <see cref="GitTasksHelper"/> using real Git repositories.
/// </summary>
public class GitTasksHelperTests
{
    #region Tag & Commit Counter Tests

    [Fact]
    public async Task DoesGitTagExist_ShouldReturnTrue_WhenTagExistsInRepository()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "v1.0.0");

        // Act
        var tagExists = GitTasksHelper.DoesGitTagExist(ws.RootDirectory, "v1.0.0");
        var nonExistentTagExists = GitTasksHelper.DoesGitTagExist(ws.RootDirectory, "v9.9.9");

        // Assert
        Assert.True(tagExists);
        Assert.False(nonExistentTagExists);
    }

    [Fact]
    public async Task GetCommitCount_ShouldReturnAccurateNumberOfCommits()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CommitAllAsync("First commit");
        await ws.CommitAllAsync("Second commit");
        await ws.CommitAllAsync("Third commit");

        // Act
        var count = GitTasksHelper.GetCommitCount(ws.RootDirectory);

        // Assert
        Assert.Equal(3, count);
    }

    #endregion

    #region Detailed Tests for HasBreakingCommitMarker

    [Theory]
    [InlineData("feat!: breaking change without scope", true)]
    [InlineData("feat(CoreLib)!: breaking change with scope", true)]
    [InlineData("FIX(CoreLib)!: uppercase breaking change", true)]
    [InlineData("refactor(api/v1)!: breaking change with nested scope path", true)]
    [InlineData("chore: routine cleanup\n\nBREAKING CHANGE: removed deprecated method", true)]
    [InlineData("feat: update response model\n\nBREAKING-CHANGE: changed property type from int to string", true)]
    [InlineData("docs: update API guide\n\nbreaking change: lower case footer marker", true)]
    [InlineData("feat(CoreLib): standard feature addition", false)]
    [InlineData("fix(CoreLib): resolve issue with breaking animation rendering", false)]
    [InlineData("style: format code style", false)]
    public async Task HasBreakingCommitMarker_ShouldCorrectlyClassifyCommitMessages(string commitMessage, bool expectedResult)
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Base version");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0");

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Modified version");
        await ws.CommitAllAsync(commitMessage);

        // Act
        var result = GitTasksHelper.HasBreakingCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task HasBreakingCommitMarker_ShouldIsolateByProjectDirectory()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// CoreLib code");
        await ws.CreateTextFileAsync("src/AppLib/Class1.cs", "// AppLib code");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0", "AppLib-v1.0.0");

        // Add breaking change commit ONLY to CoreLib directory
        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Breaking CoreLib change");
        await ws.CommitAllAsync("feat(CoreLib)!: breaking change in CoreLib");

        // Act
        var coreLibHasBreaking = GitTasksHelper.HasBreakingCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");
        var appLibHasBreaking = GitTasksHelper.HasBreakingCommitMarker(ws.RootDirectory, (RelativePath)"src/AppLib", "AppLib-v1.0.0");

        // Assert
        Assert.True(coreLibHasBreaking);
        Assert.False(appLibHasBreaking); // AppLib directory was untouched by breaking commit
    }

    [Fact]
    public async Task HasBreakingCommitMarker_ShouldCheckAllCommits_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Initial commit without tag");
        await ws.CommitAllAsync("feat(CoreLib)!: initial breaking change in untagged repo");

        // Act
        var hasBreaking = GitTasksHelper.HasBreakingCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", baselineTag: null);

        // Assert
        Assert.True(hasBreaking);
    }

    #endregion

    #region Detailed Tests for HasFeatureCommitMarker

    [Theory]
    [InlineData("feat: add new feature without scope", true)]
    [InlineData("feat(CoreLib): add helper extension method", true)]
    [InlineData("FEAT(CoreLib): uppercase feature commit", true)]
    [InlineData("feat(api/v2): add new endpoints for version 2", true)]
    [InlineData("feat(CoreLib)!: breaking feature addition", true)]
    [InlineData("fix(CoreLib): standard bug fix", false)]
    [InlineData("chore: update build scripts", false)]
    [InlineData("docs: update API usage documentation", false)]
    [InlineData("refactor(CoreLib): simplify internal logic", false)]
    [InlineData("feature: non-standard prefix should not match", false)]
    public async Task HasFeatureCommitMarker_ShouldCorrectlyClassifyCommitMessages(string commitMessage, bool expectedResult)
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Base code");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0");

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Updated feature code");
        await ws.CommitAllAsync(commitMessage);

        // Act
        var result = GitTasksHelper.HasFeatureCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task HasFeatureCommitMarker_ShouldIsolateByProjectDirectory()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// CoreLib code");
        await ws.CreateTextFileAsync("src/AppLib/Class1.cs", "// AppLib code");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0", "AppLib-v1.0.0");

        // Add feature commit ONLY to CoreLib directory
        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// New feature in CoreLib");
        await ws.CommitAllAsync("feat(CoreLib): add new service");

        // Act
        var coreLibHasFeature = GitTasksHelper.HasFeatureCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");
        var appLibHasFeature = GitTasksHelper.HasFeatureCommitMarker(ws.RootDirectory, (RelativePath)"src/AppLib", "AppLib-v1.0.0");

        // Assert
        Assert.True(coreLibHasFeature);
        Assert.False(appLibHasFeature); // AppLib was not affected by the feature commit
    }

    [Fact]
    public async Task HasFeatureCommitMarker_ShouldCheckAllCommits_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Initial feature commit without tag");
        await ws.CommitAllAsync("feat(CoreLib): initial feature in untagged repository");

        // Act
        var hasFeature = GitTasksHelper.HasFeatureCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", baselineTag: null);

        // Assert
        Assert.True(hasFeature);
    }

    #endregion

    #region Baseline Tag Range Tests (baselineTag == null)

    [Fact]
    public async Task HasCommitsSince_ShouldEvaluateAgainstHEAD_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Initial commit without tag");
        await ws.CommitAllAsync("feat(CoreLib): initial untagged commit");

        // Act: Evaluate with baselineTag = null (ranges to HEAD)
        var hasCommits = GitTasksHelper.HasCommitsSince(ws.RootDirectory, (RelativePath)"src/CoreLib", baselineTag: null);

        // Assert
        Assert.True(hasCommits);
    }

    [Fact]
    public async Task HasBreakingCommitMarker_ShouldDetectMarker_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Breaking API");
        await ws.CommitAllAsync("feat(CoreLib)!: initial breaking change");

        // Act
        var hasBreaking = GitTasksHelper.HasBreakingCommitMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", baselineTag: null);

        // Assert
        Assert.True(hasBreaking);
    }

    [Fact]
    public async Task HasSecurityFixMarker_ShouldDetectMarker_WhenBaselineTagIsNull()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Fix vulnerability");
        await ws.CommitAllAsync("fix(CoreLib): patch CVE-2024-54321 issue");

        // Act
        var hasSecurityFix = GitTasksHelper.HasSecurityFixMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", baselineTag: null);

        // Assert
        Assert.True(hasSecurityFix);
    }

    #endregion

    #region Security Fix Regex Matcher Tests

    [Theory]
    [InlineData("fix(CoreLib): resolve CVE-2024-12345 vulnerability", true)]
    [InlineData("sec(CoreLib): patch GHSA-1234-abcd-5678 advisory", true)]
    [InlineData("fix: mitigate XSS vulnerability in public endpoint", true)]
    [InlineData("chore: patch SQLi exploit in database layer", true)]
    [InlineData("fix(security): prevent CSRF token bypass", true)]
    [InlineData("fix: eliminate potential RCE vector", true)]
    [InlineData("build: security patch update for dependencies", true)]
    [InlineData("fix(CoreLib): standard bug fix without security context", false)]
    [InlineData("docs: update README file", false)]
    public async Task HasSecurityFixMarker_ShouldMatchVariousSecurityKeywordsAndIdentifiers(
        string commitMessage,
        bool expectedResult)
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Initial code");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0");

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Modified code");
        await ws.CommitAllAsync(commitMessage);

        // Act
        var hasSecurityFix = GitTasksHelper.HasSecurityFixMarker(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");

        // Assert
        Assert.Equal(expectedResult, hasSecurityFix);
    }

    #endregion

    #region Git Inspection Tests (GitLog, GitShow & GitDiff)

    [Fact]
    public async Task GitLog_ShouldReturnFormattedCommitLogEntries()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Initial code");
        await ws.CreateFileCommitWithTagsAsync("Versions.props", "<Project />", "CoreLib-v1.0.0");

        await ws.CreateTextFileAsync("src/CoreLib/Class1.cs", "// Updated code");
        await ws.CommitAllAsync("feat(CoreLib): add new functionality");

        // Act
        var logOutput = GitTasksHelper.GitLog(ws.RootDirectory, (RelativePath)"src/CoreLib", "CoreLib-v1.0.0");
        var logText = string.Join("\n", logOutput.Select(o => o.Text));

        // Assert
        Assert.NotEmpty(logOutput);
        Assert.Contains("feat(CoreLib): add new functionality", logText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GitShow_ShouldRetrieveHistoricalFileContentAtSpecificTag()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var initialXml = "<Project><PropertyGroup><LibVersion>1.0.0</LibVersion></PropertyGroup></Project>";
        await ws.CreateFileCommitWithTagsAsync("Versions.props", initialXml, "v1.0.0");

        var updatedXml = "<Project><PropertyGroup><LibVersion>1.1.0</LibVersion></PropertyGroup></Project>";
        await ws.CreateTextFileAsync("Versions.props", updatedXml);
        await ws.CommitAllAsync("Update version to 1.1.0");

        // Act
        var showOutput = GitTasksHelper.GitShow(ws.RootDirectory, (RelativePath)"Versions.props", "v1.0.0");
        var contentAtTag = string.Join("\n", showOutput.Select(o => o.Text));

        // Assert
        Assert.Contains("<LibVersion>1.0.0</LibVersion>", contentAtTag, StringComparison.Ordinal);
        Assert.DoesNotContain("<LibVersion>1.1.0</LibVersion>", contentAtTag, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GitDiff_ShouldReturnDiffLines_WhenFileWasModifiedSinceBaselineTag()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        var baselineContent = "<PackageVersion Include=\"Newtonsoft.Json\" Version=\"12.0.1\" />";
        await ws.CreateFileCommitWithTagsAsync("Directory.Packages.props", baselineContent, "v1.0.0");

        var updatedContent = "<PackageVersion Include=\"Newtonsoft.Json\" Version=\"13.0.3\" />";
        await ws.CreateTextFileAsync("Directory.Packages.props", updatedContent);
        await ws.CommitAllAsync("cpm: bump Newtonsoft.Json to 13.0.3");

        // Act
        var diffOutput = GitTasksHelper.GitDiff(ws.RootDirectory, (RelativePath)"Directory.Packages.props", "v1.0.0");
        var diffText = string.Join("\n", diffOutput.Select(o => o.Text));

        // Assert
        Assert.NotEmpty(diffOutput);
        Assert.Contains("Newtonsoft.Json", diffText, StringComparison.Ordinal);
    }

    #endregion
}
