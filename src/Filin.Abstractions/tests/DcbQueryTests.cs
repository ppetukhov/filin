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
        Assert.True(query.IsEmpty);
        Assert.Equal(0, query.Count);
        Assert.True(query.AsSpan().IsEmpty);
    }

    [Fact]
    public void CollectionExpression_WithBracketSyntax_ShouldCompileAndSortAutomated()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypes(new EventType("B")), new Tags(new Tag("tag:b")));
        var q2 = new DcbQueryItem(new EventTypes(new EventType("A")), new Tags(new Tag("tag:a")));

        // Act
        DcbQuery query = [q1, q2];

        // Assert
        Assert.Equal(2, query.Count); // items with unique tags must remain independent within the query
        Assert.Equal(q2, query[0]); // ordered by IComparable specification ('tag:a' precedes 'tag:b')
        Assert.Equal(q1, query[1]);
    }

    [Fact]
    public void Constructor_WithIdenticalTags_ShouldMergeEventTypesAndDeduplicateItems()
    {
        // Arrange
        var sharedTags = new Tags(new Tag("course:777"));

        var q1 = new DcbQueryItem(new EventTypes(new EventType("Updated")), sharedTags);
        var q2 = new DcbQueryItem(new EventTypes(new EventType("Created")), sharedTags);

        // Act
        DcbQuery query = [q1, q2];

        // Assert
        Assert.Equal(1, query.Count);

        var mergedItem = query[0];
        Assert.Equal(sharedTags, mergedItem.Tags);
        Assert.Equal(2, mergedItem.EventTypes.Count);
        Assert.Equal(new EventType("Created"), mergedItem.EventTypes[0]);
        Assert.Equal(new EventType("Updated"), mergedItem.EventTypes[1]);
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
        Assert.False(query.Contains(target));
    }

    [Fact]
    public void Contains_WhenItemExists_ShouldReturnTrue()
    {
        // Arrange
        var q1 = new DcbQueryItem([new EventType("Created")], [new Tag("t1")]);
        var q2 = new DcbQueryItem([new EventType("Updated")], [new Tag("t2")]);
        DcbQuery query = [q2, q1];

        // Act & Assert
        Assert.True(query.Contains(q1));
        Assert.True(query.Contains(q2));
    }

    #endregion

    #region Equality, IComparable and Relational Operators

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypes(new EventType("A")), new Tags(new Tag("t1")));
        var q2 = new DcbQueryItem(new EventTypes(new EventType("B")), new Tags(new Tag("t2")));

        DcbQuery c1 = [q1, q2];
        DcbQuery c2 = [q2, q1];

        // Act & Assert
        Assert.True(c1.Equals(c2));
        Assert.True(c1 == c2);
        Assert.Equal(c2.GetHashCode(), c1.GetHashCode());
    }

    [Fact]
    public void RelationalOperators_WithVariousQueries_ShouldEvaluateCorrectly()
    {
        // Arrange
        var q1 = new DcbQueryItem(new EventTypes(new EventType("A")), new Tags(new Tag("t1")));
        var q2 = new DcbQueryItem(new EventTypes(new EventType("B")), new Tags(new Tag("t2")));

        DcbQuery c1 = [q1];
        DcbQuery c2 = [q2];
        DcbQuery c3 = [q1];

        // Act & Assert
        Assert.True(c1 == c3);
        Assert.True(c1 != c2);
        Assert.True(c1 < c2);
        Assert.True(c1 <= c3);
        Assert.True(c2 > c1);
        Assert.True(c3 >= c1);
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
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Equal("DcbQueryEnumerator.Current value has not been set.", ex.Message);
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
            Tags = new Tags(new Tag("course:777")),
            Payload = new object()
        };

        // Act & Assert
        Assert.True(query.MatchesEvent(evt), "an empty query specification implicitly allows any event stream");
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
            Tags = new Tags(new Tag("course:222")),
            Payload = new object()
        };

        // Assert
        Assert.True(query.MatchesEvent(evt), "the event satisfies the second specification block within the composite query");
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
            Tags = new Tags(new Tag("course:999")),
            Payload = new object()
        };

        // Assert
        Assert.False(query.MatchesEvent(evt), "the event does not satisfy any active consistency boundaries defined in the query");
    }

    #endregion

    #region Fluent API Pipelines (Or Modifications)

    [Fact]
    public void Or_WithSingleQueryItem_ShouldAppendAndOptimizeCorrectly()
    {
        // Arrange: Query targeting course:777
        var sharedTags = new Tags(new Tag("course:777"));
        DcbQuery query = [new DcbQueryItem(new EventTypes(new EventType("Created")), sharedTags)];

        var extraItem = new DcbQueryItem(new EventTypes(new EventType("Updated")), sharedTags);

        // Act: Appending single item via Fluent Or method
        var result = query.Or(extraItem);

        // Assert: They must merge because tags are identical
        Assert.Equal(1, result.Count);
        Assert.Equal(2, result[0].EventTypes.Count);
        Assert.Equal(new EventType("Created"), result[0].EventTypes[0]);
        Assert.Equal(new EventType("Updated"), result[0].EventTypes[1]);
    }

    [Fact]
    public void Or_WithCompositeQuery_ShouldMergeMonolithicallyAndGroupTags()
    {
        // Arrange: Two distinct queries with intersecting boundaries
        var tagsA = new Tags(new Tag("course:111"));
        var tagsB = new Tags(new Tag("course:222"));

        DcbQuery query1 = [
            new DcbQueryItem(new EventTypes(new EventType("Created")), tagsA),
            new DcbQueryItem(new EventTypes(new EventType("Created")), tagsB)
        ];

        DcbQuery query2 = [
            new DcbQueryItem(new EventTypes(new EventType("Updated")), tagsB)
        ];

        // Act: Merging two queries
        var result = query1.Or(query2);

        // Assert: Inside result, 'tagsB' should collapse into 1 item, 'tagsA' stays independent -> Count 2
        Assert.Equal(2, result.Count); // overlapping tag sectors from both queries must collapse cleanly

        // Due to default IComparable layout, 'course:111' precedes 'course:222'
        Assert.Equal(tagsA, result[0].Tags);
        Assert.Equal(1, result[0].EventTypes.Count);

        Assert.Equal(tagsB, result[1].Tags);
        Assert.Equal(2, result[1].EventTypes.Count);
        Assert.Equal(new EventType("Created"), result[1].EventTypes[0]);
        Assert.Equal(new EventType("Updated"), result[1].EventTypes[1]);
    }

    #endregion
}
