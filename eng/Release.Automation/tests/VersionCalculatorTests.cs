namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for verifying semantic version calculations using <see cref="VersionCalculator"/>.
/// </summary>
public class VersionCalculatorTests
{
    [Theory]
    // --- New Projects ---
    [InlineData("0.0.0", VersionBumpType.None, ReleaseChannel.Stable, true, false, "0.1.0")]
    [InlineData("0.0.0", VersionBumpType.None, ReleaseChannel.Stable, true, true, "1.0.0")]
    [InlineData("0.0.0", VersionBumpType.None, ReleaseChannel.Beta, true, false, "0.1.0-beta.1")]
    [InlineData("0.0.0", VersionBumpType.None, ReleaseChannel.Rc, true, true, "1.0.0-rc.1")]

    // --- Standard Stable Release Bumps (major >= 1) ---
    [InlineData("1.2.3", VersionBumpType.Patch, ReleaseChannel.Stable, false, false, "1.2.4")]
    [InlineData("1.2.3", VersionBumpType.Minor, ReleaseChannel.Stable, false, false, "1.3.0")]
    [InlineData("1.2.3", VersionBumpType.Major, ReleaseChannel.Stable, false, false, "2.0.0")]

    // --- Zero-version Stable Release Bumps (major == 0) ---
    [InlineData("0.1.2", VersionBumpType.Patch, ReleaseChannel.Stable, false, false, "0.1.3")]
    [InlineData("0.1.2", VersionBumpType.Minor, ReleaseChannel.Stable, false, false, "0.1.3")]
    [InlineData("0.1.2", VersionBumpType.Major, ReleaseChannel.Stable, false, false, "0.2.0")]

    // --- Zero-version Promotion to Major ---
    [InlineData("0.5.2", VersionBumpType.Minor, ReleaseChannel.Stable, false, true, "1.0.0")]
    [InlineData("0.9.0", VersionBumpType.Major, ReleaseChannel.Stable, false, true, "1.0.0")]

    // --- Existing Pre-release Suffix Transitions ---
    [InlineData("1.2.3-beta.1", VersionBumpType.None, ReleaseChannel.Beta, false, false, "1.2.3-beta.2")]
    [InlineData("1.2.3-beta.9", VersionBumpType.None, ReleaseChannel.Beta, false, false, "1.2.3-beta.10")]
    [InlineData("1.2.3-beta.2", VersionBumpType.None, ReleaseChannel.Rc, false, false, "1.2.3-rc.1")]
    [InlineData("1.2.3-beta.2", VersionBumpType.None, ReleaseChannel.Stable, false, false, "1.2.3")]
    [InlineData("0.5.0-beta.2", VersionBumpType.None, ReleaseChannel.Stable, false, true, "1.0.0")]
    internal async Task CalculateNextVersion_ShouldReturnExpectedVersion_ForGivenInputs(
        string currentVersion,
        VersionBumpType bumpType,
        ReleaseChannel channel,
        bool isNewProject,
        bool promoteToMajor,
        string expectedVersion)
    {
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Act
        var calculatedVersion = VersionCalculator.CalculateNextVersion(
            ws.RootDirectory,
            currentVersion,
            bumpType,
            channel,
            isNewProject,
            promoteToMajor);

        // Assert
        Assert.Equal(expectedVersion, calculatedVersion);
    }

    [Fact]
    internal async Task CalculateNextVersion_ShouldIncludeGitCommitCount_ForAlphaChannel()
    {
        // Arrange
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Create baseline tag and 1 additional commit to make commit count predictable (1 commit)
        var baselineXml = "<Project><PropertyGroup><MainAppVersion>1.0.0</MainAppVersion></PropertyGroup></Project>";
        await ws.CreateFileCommitWithTagsAsync("Versions.props", baselineXml, "v1.0.0");

        // Act
        var calculatedVersion = VersionCalculator.CalculateNextVersion(
            ws.RootDirectory,
            currentVersionString: "1.0.0",
            bumpType: VersionBumpType.Minor,
            channel: ReleaseChannel.Alpha,
            isNewProject: false,
            promoteToMajor: false);

        // Assert: 1.0.0 + Minor bump -> 1.1.0, suffix '-alpha.1' (1 commit in git history)
        Assert.Equal("1.1.0-alpha.1", calculatedVersion);
    }

    [Fact]
    internal async Task CalculateNextVersion_ShouldThrowArgumentOutOfRangeException_WhenBumpTypeIsNoneOnExistingProject()
    {
        using var ws = TestWorkspace.CreateWorkspace();
        await ws.InitGitRepositoryAsync();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            VersionCalculator.CalculateNextVersion(
                ws.RootDirectory,
                currentVersionString: "1.0.0",
                bumpType: VersionBumpType.None,
                channel: ReleaseChannel.Stable,
                isNewProject: false,
                promoteToMajor: false));
    }
}
