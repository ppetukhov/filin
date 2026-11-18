namespace Release.Automation.Tests;

/// <summary>
/// Contains unit tests for <see cref="ReleaseChannelExtensions"/> verifying release channel classification.
/// </summary>
public class ReleaseChannelExtensionsTests
{
    [Theory]
    [InlineData(ReleaseChannel.Alpha, false)]
    [InlineData(ReleaseChannel.Beta, true)]
    [InlineData(ReleaseChannel.Rc, true)]
    [InlineData(ReleaseChannel.Stable, true)]
    internal void IsExplicitRelease_ShouldReturnExpectedResult_ForGivenReleaseChannel(ReleaseChannel channel, bool expectedResult)
    {
        // Act
        var isExplicit = channel.IsExplicitRelease;

        // Assert
        Assert.Equal(expectedResult, isExplicit);
    }
}
