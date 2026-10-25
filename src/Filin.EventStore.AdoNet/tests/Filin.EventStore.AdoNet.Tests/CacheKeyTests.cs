using Filin.EventStore.AdoNet.Internal;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public class CacheKeyTests
{
    [Fact]
    public void TryCreate_WithValidSingleEventAndQuery_ReturnsTrueAndCorrectInternalLayout()
    {
        // Arrange
        var dcbEvent = new DcbEvent
        {
            EventType = new EventType("TestEvent"),
            Tags = Tags.Create([new Tag("T1"), new Tag("T2"), new Tag("T3")]),
            Payload = new object()
        };

        var queryItem = new DcbQueryItem(
            new EventTypes([new EventType("E1"), new EventType("E2")]),
            Tags.Create([new Tag("A"), new Tag("B"), new Tag("C"), new Tag("D")])
        );
        DcbQuery query = [queryItem];

        // Act
        var success = CacheKey.TryCreate(dcbEvent, query, out var key);

        // Assert
        Assert.True(success);
        Assert.NotEqual(default, key);

        var eventTags = CacheKey.EventTagsCount(ref key);
        Assert.Equal(1, eventTags.Length);
        Assert.Equal(3, eventTags[0]);

        var failIfMatch = CacheKey.FailIfMatch(ref key);
        Assert.Equal(1, failIfMatch.Length);
        Assert.Equal(2, failIfMatch[0].EventTypesCount);
        Assert.Equal(4, failIfMatch[0].TagsCount);
    }

    [Fact]
    public void TryCreate_WithCollectionAndQuery_ReturnsTrueAndSavesAllEventTags()
    {
        // Arrange
        var events = new List<DcbEvent>
        {
            new() { EventType = "Ev1", Tags = Tags.Create([new Tag("X"), new Tag("Y")]), Payload = new object() },
            new() { EventType = "Ev2", Tags = Tags.Create([new Tag("Z")]), Payload = new object() }
        };

        var queryItem = new DcbQueryItem(new EventTypes(["E1"]), Tags.Create([new Tag("A")]));
        DcbQuery query = [queryItem];

        // Act
        var success = CacheKey.TryCreate(events, query, out var key);

        // Assert
        Assert.True(success);

        var eventTags = CacheKey.EventTagsCount(ref key);
        Assert.Equal(2, eventTags.Length);
        Assert.Equal(2, eventTags[0]);
        Assert.Equal(1, eventTags[1]);

        var failIfMatch = CacheKey.FailIfMatch(ref key);
        Assert.Equal(1, failIfMatch.Length);
        Assert.Equal(1, failIfMatch[0].EventTypesCount);
        Assert.Equal(1, failIfMatch[0].TagsCount);
    }

    [Fact]
    public void TryCreate_WithOnlyQuery_ReturnsTrueAndZeroEventsCount()
    {
        // Arrange
        var queryItem1 = new DcbQueryItem(new EventTypes(["E1"]), Tags.Create([new Tag("A")]));
        var queryItem2 = new DcbQueryItem(new EventTypes(["E2", "E3"]), Tags.Create([new Tag("B"), new Tag("C")]));
        DcbQuery query = [queryItem1, queryItem2];

        // Act
        var success = CacheKey.TryCreate(query, out var key);

        // Assert
        Assert.True(success);

        var eventTags = CacheKey.EventTagsCount(ref key);
        Assert.True(eventTags.IsEmpty);

        var failIfMatch = CacheKey.FailIfMatch(ref key);
        Assert.Equal(2, failIfMatch.Length);

        Assert.Equal(1, failIfMatch[0].EventTypesCount);
        Assert.Equal(1, failIfMatch[0].TagsCount);

        Assert.Equal(2, failIfMatch[1].EventTypesCount);
        Assert.Equal(2, failIfMatch[1].TagsCount);
    }

    [Fact]
    public void TryCreate_WhenSizeExceedsMaximum16Bytes_ReturnsFalseAndDefaultKey()
    {
        // Arrange
        var dcbEvent = new DcbEvent { EventType = "Ev", Tags = Tags.Create([new Tag("T")]), Payload = new object() };

        var queryItems = new List<DcbQueryItem>();
        for (int i = 0; i < 7; i++)
        {
            queryItems.Add(new DcbQueryItem(
                new EventTypes(["E"]),
                Tags.Create([new Tag($"UniqueTag{i}")])
            ));
        }
        DcbQuery oversizedQuery = DcbQuery.Create(queryItems.ToArray());

        // Act
        var success = CacheKey.TryCreate(dcbEvent, oversizedQuery, out var key);

        // Assert
        Assert.False(success);
        Assert.Equal(default, key);
    }

    [Fact]
    public void Equals_AndHashCode_IdenticalPayloads_ReturnsTrueAndMatchingHashes()
    {
        // Arrange
        var dcbEvent = new DcbEvent { EventType = "Ev", Tags = Tags.Create([new Tag("T")]), Payload = new object() };
        var queryItem = new DcbQueryItem(new EventTypes(["E"]), Tags.Create([new Tag("A")]));
        DcbQuery query = [queryItem];

        CacheKey.TryCreate(dcbEvent, query, out var key1);
        CacheKey.TryCreate(dcbEvent, query, out var key2);

        // Act & Assert
        Assert.True(key1.Equals(key2));
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentPayloads_ReturnsFalse()
    {
        // Arrange
        var dcbEvent1 = new DcbEvent { EventType = "Ev", Tags = Tags.Create([new Tag("1")]), Payload = new object() };
        var dcbEvent2 = new DcbEvent { EventType = "Ev", Tags = Tags.Create([new Tag("1"), new Tag("2")]), Payload = new object() };

        var queryItem = new DcbQueryItem(new EventTypes(["E"]), Tags.Create([new Tag("A")]));
        DcbQuery query = [queryItem];

        CacheKey.TryCreate(dcbEvent1, query, out var key1);
        CacheKey.TryCreate(dcbEvent2, query, out var key2);

        // Act & Assert
        Assert.False(key1.Equals(key2));
        Assert.True(key1 != key2);
        Assert.False(key1 == key2);
        Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void Equals_WithStandardObjectOverload_ValidatesCorrectly()
    {
        // Arrange
        var queryItem = new DcbQueryItem(new EventTypes(["E"]), Tags.Create([new Tag("A")]));
        DcbQuery query = [queryItem];
        CacheKey.TryCreate(query, out var key);

        object identicalBoxed = key;
        object differentBoxed = default(CacheKey);
        object notCacheKey = "SomeString";

        // Act & Assert
        Assert.True(key.Equals(identicalBoxed));
        Assert.False(key.Equals(differentBoxed));
        Assert.False(key.Equals(notCacheKey));
        Assert.False(key.Equals(null));
    }
}
