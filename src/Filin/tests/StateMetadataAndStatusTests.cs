using Filin.Internal;

namespace Filin.Tests;

public class StateMetadataAndStatusTests
{
    [Fact]
    public void StateMetadata_ShouldInitializeCorrectly()
    {
        // Arrange
        var position = new EventPosition(100);
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var metadata = new StateMetadata(position, timestamp, StateStatus.Created);

        // Assert
        Assert.Equal(position, metadata.Position);
        Assert.Equal(timestamp, metadata.Timestamp);
        Assert.Equal(StateStatus.Created, metadata.Status);
    }
}
