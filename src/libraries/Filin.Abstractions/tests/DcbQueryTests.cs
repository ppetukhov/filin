using System.Collections.Immutable;

namespace Filin.Abstractions.Tests;

public sealed class DcbQueryTests
{
    [Fact]
    public void DcbQuery_Contructor()
    {
        ImmutableHashSet<DcbQueryItem> expected = [
            new DcbQueryItem(new EventType("SomeEventType")),
            new DcbQueryItem(new EventType("OtherEventType")),
        ];

        var result = new DcbQuery(expected);

        result.Items.Should().BeSameAs(expected);
    }

    [Fact]
    public void DcbQuery_FromItems()
    {
        List<DcbQueryItem> items = [
            new DcbQueryItem(new EventType("SomeEventType")),
            new DcbQueryItem(new EventType("OtherEventType")),
        ];

        var result = DcbQuery.FromItems(items);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(items[0]);
        result.Items.Should().Contain(items[1]);
    }

    [Fact]
    public void DcbQuery_FromItems_With_Any_EventTypes()
    {
        var result = DcbQuery.FromItems([
            new DcbQueryItem(new EventType("SomeEventType")),
            DcbQueryItem.Any
        ]);

        result.IsAll.Should().BeTrue();
        result.Should().BeSameAs(DcbQuery.All);
    }

    [Fact]
    public void DcbQuery_IsAll_Return_True_When_Empty_QueryItems()
    {
        var result = new DcbQuery([]);

        result.IsAll.Should().BeTrue();
    }

    [Fact]
    public void DcbQuery_IsAll_Return_True_When_QueryItems_For_Any_EventTypes()
    {
        var result = new DcbQuery([
            new DcbQueryItem(new EventType("SomeEventType")),
            DcbQueryItem.Any
        ]);

        result.IsAll.Should().BeTrue();
    }

    [Theory]
    [InlineData("EventB", "Tag1", "Tag2")]
    [InlineData("EventC", "Tag1", "Tag3", "Tag4")]
    [InlineData("EventE", "Tag1", "Tag2", "Tag3", "Tag4", "Tag5")]
    [InlineData("OnyEventType", "Tag1", "Tag4", "Tag5")]
    [InlineData("EventF")]
    [InlineData("EventF", "Tag1", "Tag2", "Tag5")]
    public void DcbQuery_MatchesEvent_Return_True(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };

        var query = new DcbQuery([
            new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]),
            new DcbQueryItem(["EventC", "EventD"], ["Tag1", "Tag3", "Tag4"]),
            new DcbQueryItem(["EventE"], ["Tag3", "Tag4"]),
            new DcbQueryItem([], ["Tag1", "Tag4", "Tag5"]),
            new DcbQueryItem(["EventF"], []),
        ]);

        var result = query.MatchesEvent(dcbEvent);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("EventA")]
    [InlineData("EventB", "Tag2", "Tag3")]
    [InlineData("EventD", "Tag1", "Tag3")]
    [InlineData("OnyEventType", "Tag2", "Tag3", "Tag4", "Tag5")]
    [InlineData("OnyEventType", "Tag2", "Tag4", "Tag6")]
    public void DcbQuery_MatchesEvent_Return_False(string eventTypeName, params string[] tagNames)
    {
        var dcbEvent = new DcbEvent { EventType = new(eventTypeName), Tags = [.. tagNames.Select(x => new Tag(x))], Timestamp = default, Data = new object() };

        var query = new DcbQuery([
            new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]),
            new DcbQueryItem(["EventC", "EventD"], ["Tag1", "Tag3", "Tag4"]),
            new DcbQueryItem(["EventE"], ["Tag3", "Tag4"]),
            new DcbQueryItem([], ["Tag1", "Tag4", "Tag5"]),
            new DcbQueryItem(["EventF"], []),
        ]);

        var result = query.MatchesEvent(dcbEvent);

        result.Should().BeFalse();
    }

    [Fact]
    public void DcbQuery_Or_DcbQueryItem_Return_All_When_Query_Is_All()
    {
        var queryItem = new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]);

        var result = DcbQuery.All.Or(queryItem);

        result.IsAll.Should().BeTrue();
    }

    [Fact]
    public void DcbQuery_Or_DcbQueryItem_With_AnyEventTypes_Return_All()
    {
        var query = new DcbQuery([new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"])]);

        var result = query.Or(DcbQueryItem.Any);

        result.IsAll.Should().BeTrue();
    }

    [Fact]
    public void DcbQuery_Or_DcbQueryItem_Return_Query_With_Union_Items()
    {
        var queryItem1 = new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]);
        var queryItem2 = new DcbQueryItem(["EventC"], ["Tag1", "Tag2"]);

        var query = new DcbQuery([queryItem1]);

        var result = query.Or(queryItem2);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(queryItem1);
        result.Items.Should().Contain(queryItem2);
    }

    [Fact]
    public void DcbQuery_Or_DcbQuery_Return_All_When_Query_Is_All()
    {
        var query = new DcbQuery([new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"])]);

        var result = DcbQuery.All.Or(query);

        result.IsAll.Should().BeTrue();
    }

    [Fact]
    public void DcbQuery_Or_DcbQuery_For_All_Return_All()
    {
        var query = new DcbQuery([new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"])]);

        var result = query.Or(DcbQuery.All);

        result.IsAll.Should().BeTrue();
    }

    [Fact]
    public void DcbQuery_Or_DcbQuery_Return_Query_With_Union_Items()
    {
        var queryItem1 = new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]);
        var queryItem2 = new DcbQueryItem(["EventC"], ["Tag1", "Tag2"]);
        var queryItem3 = new DcbQueryItem(["EventD"], ["Tag2"]);

        var query = new DcbQuery([queryItem1, queryItem2]);
        var query2 = new DcbQuery([queryItem2, queryItem3]);

        var result = query.Or(query2);

        result.Items.Should().HaveCount(3);
        result.Items.Should().Contain(queryItem1);
        result.Items.Should().Contain(queryItem2);
        result.Items.Should().Contain(queryItem3);
    }

    [Fact]
    public void DcbQuery_Compact_Return_Compacted_Query()
    {
        var queryItem1 = new DcbQueryItem(["EventA", "EventB"], ["Tag1", "Tag2"]);
        var queryItem2 = new DcbQueryItem(["EventC"], ["Tag1", "Tag2"]);
        var queryItem3 = new DcbQueryItem(["EventD"], ["Tag2"]);

        var query = new DcbQuery([queryItem1, queryItem2, queryItem3]);

        var result = query.Compact();

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainEquivalentOf(new DcbQueryItem(["EventA", "EventB", "EventC"], ["Tag1", "Tag2"]));
        result.Items.Should().ContainEquivalentOf(queryItem3);
    }

    [Fact]
    public void DcbQuery_FromImmutableHashSet_Return_Query()
    {
        ImmutableHashSet<DcbEvent> dcbEvents = [
            new DcbEvent { EventType = new EventType("SomeEventType"), Tags = ["Tag1"], Timestamp = default, Data = new object() },
            new DcbEvent { EventType = new EventType("OtherEventType"), Tags = ["Tag2", "Tag3"], Timestamp = default, Data = new object() }
        ];

        var result = DcbQuery.FromImmutableHashSet(dcbEvents);

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainEquivalentOf(new DcbQueryItem(["SomeEventType"], ["Tag1"]));
        result.Items.Should().ContainEquivalentOf(new DcbQueryItem(["OtherEventType"], ["Tag2", "Tag3"]));
    }
}
