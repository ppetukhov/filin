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
        queryItem.IsEmpty.Should().BeTrue();
        queryItem.Tags.IsEmpty.Should().BeTrue();
        queryItem.EventTypes.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void PrimaryConstructor_WithExplicitArgs_ShouldInitializeInCorrectOrder()
    {
        // Arrange
        var types = new EventTypeCollection(new EventType("Created"));
        var tags = new TagCollection(new Tag("course:777"));

        // Act (Invoking Primary Constructor with eventTypes first, tags second)
        var queryItem = new DcbQueryItem(types, tags);

        // Assert
        queryItem.EventTypes.Should().Be(types);
        queryItem.Tags.Should().Be(tags);
    }

    #endregion

    #region Fluent API Pipelines (And / Or Modifications)

    [Fact]
    public void Or_WithEventTypesSpan_ShouldMergeSortAndDeduplicateCorrectly()
    {
        // Arrange
        var initialTypes = new EventTypeCollection(new EventType("Created"), new EventType("Updated"));
        var initialTags = new TagCollection(new Tag("course:777"));
        var queryItem = new DcbQueryItem(initialTypes, initialTags);

        var extraType1 = new EventType("Deleted");
        var extraType2 = new EventType("Updated");

        // Act
        DcbQueryItem result = queryItem.Or(extraType1, extraType2);

        // Assert
        result.Tags.Should().Be(queryItem.Tags);
        result.EventTypes.Count.Should().Be(3);
        result.EventTypes[0].Should().Be(new EventType("Created"));
        result.EventTypes[1].Should().Be(new EventType("Deleted"));
        result.EventTypes[2].Should().Be(new EventType("Updated"));
    }

    [Fact]
    public void And_WithTagsSpan_ShouldMergeSortAndDeduplicateCorrectly()
    {
        // Arrange
        var initialTypes = new EventTypeCollection(new EventType("Created"));
        var initialTags = new TagCollection(new Tag("faculty:it"), new Tag("student:123"));
        var queryItem = new DcbQueryItem(initialTypes, initialTags);

        var extraTag1 = new Tag("course:777");
        var extraTag2 = new Tag("student:123");

        // Act
        DcbQueryItem result = queryItem.And(extraTag1, extraTag2);

        // Assert
        result.EventTypes.Should().Be(queryItem.EventTypes);
        result.Tags.Count.Should().Be(3);
        result.Tags[0].Should().Be(new Tag("course:777"));
        result.Tags[1].Should().Be(new Tag("faculty:it"));
        result.Tags[2].Should().Be(new Tag("student:123"));
    }

    #endregion

    #region Event Stream Filtering (MatchesEvent Strategy)

    [Fact]
    public void MatchesEvent_WhenQueryTagsAreSubsetOfEventTags_ShouldReturnTrue()
    {
        // Arrange
        var queryItem = new DcbQueryItem(
            new EventTypeCollection(new EventType("CourseCapacityChanged")),
            new TagCollection(new Tag("a"), new Tag("c"))
        );

        // Act
        var evt = new DcbEvent
        {
            EventType = new EventType("CourseCapacityChanged"),
            Timestamp = DateTimeOffset.UtcNow,
            Tags = new TagCollection(new Tag("a"), new Tag("b"), new Tag("c")),
            Data = new object()
        };

        // Assert
        queryItem.MatchesEvent(evt).Should().BeTrue();
    }

    #endregion
}
