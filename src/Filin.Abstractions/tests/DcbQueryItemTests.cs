namespace Filin.Abstractions.Tests;

public class DcbQueryItemTests
{
    #region Structural Initialization and Any/IsAny States

    [Fact]
    public void Constructor_EmptyCollections_ShouldCreateEmptyQueryItem()
    {
        // Act
        var queryItem = new DcbQueryItem();

        // Assert
        Assert.True(queryItem.IsEmpty);
        Assert.True(queryItem.Tags.IsEmpty);
        Assert.True(queryItem.EventTypes.IsEmpty);
    }

    [Fact]
    public void PrimaryConstructor_WithExplicitArgs_ShouldInitializeInCorrectOrder()
    {
        // Arrange
        var types = new EventTypes(new EventType("Created"));
        var tags = new Tags(new Tag("course:777"));

        // Act (Invoking Primary Constructor with eventTypes first, tags second)
        var queryItem = new DcbQueryItem(types, tags);

        // Assert
        Assert.Equal(types, queryItem.EventTypes);
        Assert.Equal(tags, queryItem.Tags);
    }

    #endregion

    #region Fluent API Pipelines (And / Or Modifications)

    [Fact]
    public void Or_WithEventTypesSpan_ShouldMergeSortAndDeduplicateCorrectly()
    {
        // Arrange
        var initialTypes = new EventTypes(new EventType("Created"), new EventType("Updated"));
        var initialTags = new Tags(new Tag("course:777"));
        var queryItem = new DcbQueryItem(initialTypes, initialTags);

        var extraType1 = new EventType("Deleted");
        var extraType2 = new EventType("Updated");

        // Act
        var result = queryItem.Or(extraType1, extraType2);

        // Assert
        Assert.Equal(queryItem.Tags, result.Tags);
        Assert.Equal(3, result.EventTypes.Count);
        Assert.Equal(new EventType("Created"), result.EventTypes[0]);
        Assert.Equal(new EventType("Deleted"), result.EventTypes[1]);
        Assert.Equal(new EventType("Updated"), result.EventTypes[2]);
    }

    [Fact]
    public void And_WithTagsSpan_ShouldMergeSortAndDeduplicateCorrectly()
    {
        // Arrange
        var initialTypes = new EventTypes(new EventType("Created"));
        var initialTags = new Tags(new Tag("faculty:it"), new Tag("student:123"));
        var queryItem = new DcbQueryItem(initialTypes, initialTags);

        var extraTag1 = new Tag("course:777");
        var extraTag2 = new Tag("student:123");

        // Act
        var result = queryItem.And(extraTag1, extraTag2);

        // Assert
        Assert.Equal(queryItem.EventTypes, result.EventTypes);
        Assert.Equal(3, result.Tags.Count);
        Assert.Equal(new Tag("course:777"), result.Tags[0]);
        Assert.Equal(new Tag("faculty:it"), result.Tags[1]);
        Assert.Equal(new Tag("student:123"), result.Tags[2]);
    }

    #endregion

    #region Event Stream Filtering (MatchesEvent Strategy)

    [Fact]
    public void MatchesEvent_WhenQueryTagsAreSubsetOfEventTags_ShouldReturnTrue()
    {
        // Arrange
        var queryItem = new DcbQueryItem(
            new EventTypes(new EventType("CourseCapacityChanged")),
            new Tags(new Tag("a"), new Tag("c"))
        );

        // Act
        var evt = new DcbEvent
        {
            EventType = new EventType("CourseCapacityChanged"),
            Tags = new Tags(new Tag("a"), new Tag("b"), new Tag("c")),
            Payload = new object()
        };

        // Assert
        Assert.True(queryItem.MatchesEvent(evt));
    }

    #endregion
}
