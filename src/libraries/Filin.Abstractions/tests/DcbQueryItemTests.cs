using System.Collections.Immutable;

namespace Filin.Abstractions.Tests;

public sealed class DcbQueryItemTests
{
    [Fact]
    public void DcbQueryItem_Contructor_From_EventType()
    {
        var expected = new EventType("SomeEventType");

        var result = new DcbQueryItem(expected);

        result.EventTypes.Should().ContainSingle(expected);
        result.Tags.Should().BeEmpty();
    }

    [Theory]
    [InlineData("SomeEventType1")]
    [InlineData("SomeEventType1", "SomeEventType2")]
    [InlineData("SomeEventType1", "SomeEventType2", "SomeEventType3")]
    public void DcbQueryItem_Contructor_From_EventTypes(params string[] eventTypeNames)
    {
        var expectedEventTypes = eventTypeNames.Select(x => new EventType(x)).ToImmutableHashSet();

        var result = new DcbQueryItem(expectedEventTypes);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_Contructor_From_Tag()
    {
        var expected = new Tag("SomeEventTag");

        var result = new DcbQueryItem(expected);

        result.Tags.Should().ContainSingle(expected);
        result.EventTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("SomeEventTag1")]
    [InlineData("SomeEventTag1", "SomeEventTag2")]
    [InlineData("SomeEventTag1", "SomeEventTag2", "SomeEventTag3")]
    public void DcbQueryItem_Contructor_From_Tags(params string[] eventTags)
    {
        var expectedEventTags = eventTags.Select(x => new Tag(x)).ToImmutableSortedSet();

        var result = new DcbQueryItem(expectedEventTags);

        result.Tags.Should().HaveCount(expectedEventTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedEventTags);
        result.EventTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Tag1")]
    [InlineData("Event1", "Tag1")]
    [InlineData("Event1", "Tag1", "Tag2")]
    [InlineData("Event1", "Event2", "Tag1", "Tag2")]
    public void DcbQueryItem_Contructor_From_EventTypes_And_Tags(params string[] names)
    {
        var eventTypesCount = names.Length / 2;

        var expectedEventTypes = names.Take(eventTypesCount).Select(x => new EventType(x)).ToImmutableHashSet();
        var expectedEventTags = names.Skip(eventTypesCount).Select(x => new Tag(x)).ToImmutableSortedSet();

        var result = new DcbQueryItem(expectedEventTypes, expectedEventTags);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().HaveCount(expectedEventTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedEventTags);
    }

    [Fact]
    public void DcbQueryItem_IsAny()
    {
        var result = new DcbQueryItem([], []);

        result.IsAny.Should().BeTrue();
    }

    [Fact]
    public void DcbQueryItem_Any()
    {
        var result = DcbQueryItem.Any;

        result.EventTypes.Should().BeEmpty();
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_Or_EventType_From_Any()
    {
        var expected = new EventType("SomeEventType");

        var result = DcbQueryItem.Any.Or(expected);

        result.EventTypes.Should().ContainSingle(expected);
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_Or_EventType()
    {
        var expected = new EventType("ExpectedEventType");

        var result = new DcbQueryItem(new EventType("OrigEventType")).Or(expected);

        result.EventTypes.Should().HaveCount(2);
        result.EventTypes.Should().ContainEquivalentOf(expected);
        result.Tags.Should().BeEmpty();
    }

    [Theory]
    [InlineData("SomeEventType1")]
    [InlineData("SomeEventType1", "SomeEventType2")]
    [InlineData("SomeEventType1", "SomeEventType2", "SomeEventType3")]
    public void DcbQueryItem_Or_EventTypes(params string[] eventTypeNames)
    {
        var orEventTypes = eventTypeNames.Select(x => new EventType(x)).ToImmutableHashSet();
        var expected = orEventTypes.Add(new EventType("OrigEventType"));

        var result = new DcbQueryItem(new EventType("OrigEventType")).Or(orEventTypes);

        result.EventTypes.Should().HaveCount(orEventTypes.Count + 1);
        result.EventTypes.Should().ContainInConsecutiveOrder(expected);
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_And_Tag_From_Any()
    {
        var expected = new Tag("ExpectedTag");

        var result = DcbQueryItem.Any.And(expected);

        result.Tags.Should().ContainSingle(expected);
        result.EventTypes.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_And_Tag()
    {
        var expected = new Tag("ExpectedTag");

        var result = new DcbQueryItem(new Tag("OrigTag")).And(expected);

        result.Tags.Should().HaveCount(2);
        result.Tags.Should().ContainEquivalentOf(expected);
        result.EventTypes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Tag1")]
    [InlineData("Tag1", "Tag2")]
    [InlineData("Tag1", "Tag2", "Tag3")]
    public void DcbQueryItem_And_Tags(params string[] tagNames)
    {
        var andTags = tagNames.Select(x => new Tag(x)).ToImmutableHashSet();
        var expected = andTags.Add(new Tag("OrigTag")).ToList();
        expected.Sort();

        var result = new DcbQueryItem(new Tag("OrigTag")).And(andTags);

        result.Tags.Should().HaveCount(andTags.Count + 1);
        result.Tags.Should().ContainInConsecutiveOrder(expected);
        result.EventTypes.Should().BeEmpty();
    }

    [Fact]
    public void DcbQueryItem_And_EventType_With_Tag()
    {
        var andTag = new Tag("AndTag");
        var andEventType = new EventType("AndEventType");

        ImmutableHashSet<EventType> expectedEventTypes = [new EventType("OrigEventType"), andEventType];
        List<Tag> expectedTags = [new Tag("OrigTag"), andTag];
        expectedTags.Sort();

        var result = new DcbQueryItem([new EventType("OrigEventType")], [new Tag("OrigTag")]).And(andEventType, andTag);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().HaveCount(expectedTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedTags);
    }

    [Theory]
    [InlineData("Tag1")]
    [InlineData("Tag1", "Tag2")]
    [InlineData("Tag1", "Tag2", "Tag3")]
    [InlineData("Tag1", "Tag2", "Tag3", "Tag4")]
    public void DcbQueryItem_And_EventType_With_Tags(params string[] eventTags)
    {
        var andTags = eventTags.Select(x => new Tag(x)).ToImmutableHashSet();
        var andEventType = new EventType("AndEventType");

        ImmutableHashSet<EventType> expectedEventTypes = [new EventType("OrigEventType"), andEventType];
        List<Tag> expectedTags = [new Tag("OrigTag"), .. andTags];
        expectedTags.Sort();

        var result = new DcbQueryItem([new EventType("OrigEventType")], [new Tag("OrigTag")]).And(andEventType, andTags);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().HaveCount(expectedTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedTags);
    }

    [Theory]
    [InlineData("Event1")]
    [InlineData("Event1", "Event2")]
    [InlineData("Event1", "Event2", "Event3")]
    [InlineData("Event1", "Event2", "Event3", "Event4")]
    public void DcbQueryItem_And_EventTypes_With_Tag(params string[] eventTypes)
    {
        var andEventTypes = eventTypes.Select(x => new EventType(x)).ToImmutableHashSet();
        var andTag = new Tag("AndTag");

        ImmutableHashSet<EventType> expectedEventTypes = [new EventType("OrigEventType"), .. andEventTypes];
        List<Tag> expectedTags = [new Tag("OrigTag"), andTag];
        expectedTags.Sort();

        var result = new DcbQueryItem([new EventType("OrigEventType")], [new Tag("OrigTag")]).And(andEventTypes, andTag);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().HaveCount(expectedTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedTags);
    }

    [Theory]
    [InlineData("AndTag1")]
    [InlineData("AndEvent1", "AndTag1")]
    [InlineData("AndEvent1", "AndTag1", "AndTag2")]
    [InlineData("AndEvent1", "AndEvent2", "AndTag1", "AndTag2")]
    public void DcbQueryItem_And_EventTypes_With_Tags(params string[] names)
    {
        var eventTypesCount = names.Length / 2;

        var andEventTypes = names.Take(eventTypesCount).Select(x => new EventType(x)).ToImmutableHashSet();
        var andTags = names.Skip(eventTypesCount).Select(x => new Tag(x)).ToImmutableHashSet();

        ImmutableHashSet<EventType> expectedEventTypes = [new EventType("OrigEventType"), .. andEventTypes];
        List<Tag> expectedTags = [new Tag("OrigTag"), .. andTags];
        expectedTags.Sort();

        var result = new DcbQueryItem([new EventType("OrigEventType")], [new Tag("OrigTag")]).And(andEventTypes, andTags);

        result.EventTypes.Should().HaveCount(expectedEventTypes.Count);
        result.EventTypes.Should().ContainInConsecutiveOrder(expectedEventTypes);
        result.Tags.Should().HaveCount(expectedTags.Count);
        result.Tags.Should().ContainInConsecutiveOrder(expectedTags);
    }

    [Theory]
    [InlineData("AllCoursesPaused")]
    [InlineData("CourseCreated", "course:1000")]
    [InlineData("StudentSubscribed", "course:1000", "student:500000")]
    public void DcbQueryItem_MatchesEvent_Return_True_For_Any(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new EventType(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };

        var result = DcbQueryItem.Any.MatchesEvent(dcbEvent);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("StudentSubscribed", "course:1000", "student:500000")]
    [InlineData("StudentUnsubscribed", "course:1000", "student:500000")]
    [InlineData("StudentUnsubscribed", "course:1000", "student:500000", "student:20000")]
    public void DcbQueryItem_MatchesEvent_Return_True(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new EventType(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };
        var queryItem = new DcbQueryItem([new EventType("StudentSubscribed"), new EventType("StudentUnsubscribed")],
                [new Tag("course:1000"), new Tag("student:500000")]);

        var result = queryItem.MatchesEvent(dcbEvent);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("AnyEvent", "course:1000", "student:500000")]
    [InlineData("StudentSubscribed", "course:1000", "student:500000")]
    [InlineData("StudentUnsubscribed", "course:1000", "student:500000")]
    [InlineData("CourseCapacityChanged", "course:1000", "student:500000", "teacher:84562")]
    public void DcbQueryItem_MatchesEvent_Return_True_ByTags(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new EventType(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };
        var queryItem = new DcbQueryItem([new Tag("course:1000"), new Tag("student:500000")]);

        var result = queryItem.MatchesEvent(dcbEvent);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("StudentSubscribed", "course:10", "student:50")]
    [InlineData("StudentUnsubscribed", "course:1000", "student:5000")]
    [InlineData("StudentUnsubscribed", "course:9000", "student:156", "student:84661")]
    public void DcbQueryItem_MatchesEvent_Return_True_ByEventTypes(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new EventType(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };
        var queryItem = new DcbQueryItem([new EventType("StudentSubscribed"), new EventType("StudentUnsubscribed")]);

        var result = queryItem.MatchesEvent(dcbEvent);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("StudentSubscribed", "course:9000", "student:500000")]
    [InlineData("StudentUnsubscribed", "course:1000", "student:600000")]
    [InlineData("CourseCreated", "course:1000")]
    public void DcbQueryItem_MatchesEvent_Return_False(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new EventType(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };
        var queryItem = new DcbQueryItem([new EventType("StudentSubscribed"), new EventType("StudentUnsubscribed")],
                [new Tag("course:1000"), new Tag("student:500000")]);

        var result = queryItem.MatchesEvent(dcbEvent);

        result.Should().BeFalse();
    }

    [Fact]
    public void DcbQueryItem_ToDcbQuery()
    {
        var queryItem = new DcbQueryItem([new EventType("StudentSubscribed"), new EventType("StudentUnsubscribed")],
                [new Tag("course:1000"), new Tag("student:500000")]);

        var result = queryItem.ToDcbQuery();

        result.Items.Should().Contain(queryItem);
        result.Items.Should().ContainSingle();
    }
}
