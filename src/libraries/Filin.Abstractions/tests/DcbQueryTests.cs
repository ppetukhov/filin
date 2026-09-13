namespace Filin.Abstractions.Tests;

public class DcbQueryTests
{
    #region Constructor, IsEmpty and Count

    [Fact]
    public void Constructor_EmptyParams_ShouldCreateEmptyQuery()
    {
        // Act
        var query = new DcbQuery();

        // Assert
        query.IsEmpty.Should().BeTrue();
        query.Count.Should().Be(0);
        query.AsSpan().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CollectionExpression_WithBracketSyntax_ShouldCompileAndSortAutomated()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypeCollection(new EventType("B")), new TagCollection(new Tag("tag:b")));
        var q2 = new DcbQueryItem(new EventTypeCollection(new EventType("A")), new TagCollection(new Tag("tag:a")));

        // Act
        DcbQuery query = [q1, q2];

        // Assert
        query.Count.Should().Be(2, "items with unique tags must remain independent within the query");
        query[0].Should().Be(q2, "ordered by IComparable specification ('tag:a' precedes 'tag:b')");
        query[1].Should().Be(q1);
    }

    [Fact]
    public void Constructor_WithIdenticalTags_ShouldMergeEventTypesAndDeduplicateItems()
    {
        // Arrange
        var sharedTags = new TagCollection(new Tag("course:777"));

        var q1 = new DcbQueryItem(new EventTypeCollection(new EventType("Updated")), sharedTags);
        var q2 = new DcbQueryItem(new EventTypeCollection(new EventType("Created")), sharedTags);

        // Act
        DcbQuery query = [q1, q2];

        // Assert
        query.Count.Should().Be(1, "items with identical tags must be merged into a single optimized boundary item inside the query");

        var mergedItem = query[0];
        mergedItem.Tags.Should().Be(sharedTags);
        mergedItem.EventTypes.Count.Should().Be(2);
        mergedItem.EventTypes[0].Should().Be(new EventType("Created"));
        mergedItem.EventTypes[1].Should().Be(new EventType("Updated"));
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_OnEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var query = new DcbQuery();
        var target = new DcbQueryItem([new EventType("Any")]);

        // Act & Assert
        query.Contains(target).Should().BeFalse();
    }

    [Fact]
    public void Contains_WhenItemExists_ShouldReturnTrue()
    {
        // Arrange
        var q1 = new DcbQueryItem([new EventType("Created")], [new Tag("t1")]);
        var q2 = new DcbQueryItem([new EventType("Updated")], [new Tag("t2")]);
        DcbQuery query = [q2, q1];

        // Act & Assert
        query.Contains(q1).Should().BeTrue();
        query.Contains(q2).Should().BeTrue();
    }

    #endregion

    #region Equality, IComparable and Relational Operators

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypeCollection(new EventType("A")), new TagCollection(new Tag("t1")));
        var q2 = new DcbQueryItem(new EventTypeCollection(new EventType("B")), new TagCollection(new Tag("t2")));

        DcbQuery c1 = [q1, q2];
        DcbQuery c2 = [q2, q1];

        // Act & Assert
        c1.Equals(c2).Should().BeTrue();
        (c1 == c2).Should().BeTrue();
        c1.GetHashCode().Should().Be(c2.GetHashCode());
    }

    [Fact]
    public void RelationalOperators_WithVariousQueries_ShouldEvaluateCorrectly()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypeCollection(new EventType("A")), new TagCollection(new Tag("t1")));
        var q2 = new DcbQueryItem(new EventTypeCollection(new EventType("B")), new TagCollection(new Tag("t2")));

        DcbQuery c1 = [q1];
        DcbQuery c2 = [q2];
        DcbQuery c3 = [q1];

        // Act & Assert
        (c1 == c3).Should().BeTrue();
        (c1 != c2).Should().BeTrue();
        (c1 < c2).Should().BeTrue();
        (c1 <= c3).Should().BeTrue();
        (c2 > c1).Should().BeTrue();
        (c3 >= c1).Should().BeTrue();
    }

    #endregion

    #region Enumerator Testing

    [Fact]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var q1 = new DcbQueryItem([new EventType("A")]);
        DcbQuery query = [q1];
        var enumerator = query.GetEnumerator();
        Exception? ex = null;

        // Act
        try
        {
            _ = enumerator.Current;
        }
        catch (Exception e)
        {
            ex = e;
        }
        finally
        {
            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();
            ex.Message.Should().Be("DcbQuery.Enumerator.Current value has not been set.");
        }
    }

    #endregion

    #region Event Stream Filtering (MatchesEvent Strategy)

    [Fact]
    public void MatchesEvent_WhenQueryIsEmpty_ShouldAlwaysReturnTrue()
    {
        // Arrange: Empty query means no active constraints
        var query = new DcbQuery();
        var evt = new DcbEvent
        {
            EventType = new EventType("CourseSubscribed"),
            Tags = new TagCollection(new Tag("course:777")),
            Payload = new object()
        };

        // Act & Assert
        query.MatchesEvent(evt).Should().BeTrue("an empty query specification implicitly allows any event stream");
    }

    [Fact]
    public void MatchesEvent_WhenAtLeastOneItemMatches_ShouldReturnTrue()
    {
        // Arrange: Query targets two different isolated boundaries
        var q1 = new DcbQueryItem([new EventType("Created")], [new Tag("course:111")]);
        var q2 = new DcbQueryItem([new EventType("Updated")], [new Tag("course:222")]);
        DcbQuery query = [q1, q2];

        // Act: Event perfectly matches constraints of the second item (q2)
        var evt = new DcbEvent
        {
            EventType = new EventType("Updated"),
            Tags = new TagCollection(new Tag("course:222")),
            Payload = new object()
        };

        // Assert
        query.MatchesEvent(evt).Should().BeTrue("the event satisfies the second specification block within the composite query");
    }

    [Fact]
    public void MatchesEvent_WhenNoItemsMatch_ShouldReturnFalse()
    {
        // Arrange
        var q1 = new DcbQueryItem([new EventType("Created")], [new Tag("course:111")]);
        DcbQuery query = [q1];

        // Act: Event misses both allowed type and required boundary tags
        var evt = new DcbEvent
        {
            EventType = new EventType("Deleted"),
            Tags = new TagCollection(new Tag("course:999")),
            Payload = new object()
        };

        // Assert
        query.MatchesEvent(evt).Should().BeFalse("the event does not satisfy any active consistency boundaries defined in the query");
    }

    #endregion

    #region Fluent API Pipelines (Or Modifications)

    [Fact]
    public void Or_WithSingleQueryItem_ShouldAppendAndOptimizeCorrectly()
    {
        // Arrange: Query targeting course:777
        var sharedTags = new TagCollection(new Tag("course:777"));
        DcbQuery query = [new DcbQueryItem(new EventTypeCollection(new EventType("Created")), sharedTags)];

        var extraItem = new DcbQueryItem(new EventTypeCollection(new EventType("Updated")), sharedTags);

        // Act: Appending single item via Fluent Or method
        DcbQuery result = query.Or(extraItem);

        // Assert: They must merge because tags are identical
        result.Count.Should().Be(1, "the extra item shared tags with the initial query structure and must be merged");
        result[0].EventTypes.Count.Should().Be(2);
        result[0].EventTypes[0].Should().Be(new EventType("Created"));
        result[0].EventTypes[1].Should().Be(new EventType("Updated"));
    }

    [Fact]
    public void Or_WithCompositeQuery_ShouldMergeMonolithicallyAndGroupTags()
    {
        // Arrange: Two distinct queries with intersecting boundaries
        var tagsA = new TagCollection(new Tag("course:111"));
        var tagsB = new TagCollection(new Tag("course:222"));

        DcbQuery query1 = [
            new DcbQueryItem(new EventTypeCollection(new EventType("Created")), tagsA),
            new DcbQueryItem(new EventTypeCollection(new EventType("Created")), tagsB)
        ];

        DcbQuery query2 = [
            new DcbQueryItem(new EventTypeCollection(new EventType("Updated")), tagsB)
        ];

        // Act: Merging two queries
        DcbQuery result = query1.Or(query2);

        // Assert: Inside result, 'tagsB' should collapse into 1 item, 'tagsA' stays independent -> Count 2
        result.Count.Should().Be(2, "overlapping tag sectors from both queries must collapse cleanly");

        // Due to default IComparable layout, 'course:111' precedes 'course:222'
        result[0].Tags.Should().Be(tagsA);
        result[0].EventTypes.Count.Should().Be(1);

        result[1].Tags.Should().Be(tagsB);
        result[1].EventTypes.Count.Should().Be(2);
        result[1].EventTypes[0].Should().Be(new EventType("Created"));
        result[1].EventTypes[1].Should().Be(new EventType("Updated"));
    }

    #endregion

}
