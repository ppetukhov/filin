using Filin.Abstractions.Tests.Configuration;
using Filin.Configuration;

namespace Filin.Abstractions.Tests;

[EventType("SomeEvent")]
sealed record SomeEvent { }

[EventType("SomeEvent", 1)]
sealed record SomeEventV1 { }

[EventType("SomeEvent", 2026_02)]
sealed record SomeEventV2026_02 { }

sealed class TestDomainEvents
{
    internal static class V0
    {
        [EventType("SomeNestedEvent")]
        internal sealed record SomeNestedEvent { }
    }
}

[EventType("event-with-default-ver", 0)]
sealed record SomeEventWithDefaultVersion { }

public sealed class EventTypeAttributeTests
{
    [Fact]
    public void SomeEventGetEventTypeReturnSomeEvent()
    {
        var evt = new SomeEvent();

        Assert.Equal(new EventType("SomeEvent"), evt.GetEventType());
    }

    [Fact]
    public void SomeNestedEventGetEventTypeReturnSomeNestedEvent()
    {
        var evt = new TestDomainEvents.V0.SomeNestedEvent();

        Assert.Equal(new EventType("SomeNestedEvent"), evt.GetEventType());
    }

    [Fact]
    public void SomeEventWithDefaultVersionGetEventTypeReturnWithWithOutVersion()
    {
        var evt = new SomeEventWithDefaultVersion();

        Assert.Equal(new EventType("event-with-default-ver"), evt.GetEventType());
    }

    [Fact]
    public void SomeEventV1GetEventTypeReturnSomeEventAt1()
    {
        var evt = new SomeEventV1();

        Assert.Equal(new EventType("SomeEvent@1"), evt.GetEventType());
    }

    [Fact]
    public void SomeEventV2026_02GetEventTypeReturnSomeEventAt202602()
    {
        var evt = new SomeEventV2026_02();

        Assert.Equal(new EventType("SomeEvent@202602"), evt.GetEventType());
    }

    [Fact]
    public void MetadataEventTypes_Configure_EventTypes()
    {
        var options = new TypeManifestOptions();
        var provider = new FilinAbstractionsTestsEventTypesTypeManifestProvider();

        provider.Configure(options);

        Assert.Equal(5, options.EventTypes.Count);
        Assert.Contains(new KeyValuePair<EventType, Type>(new EventType("SomeEvent"), typeof(SomeEvent)), options.EventTypes);
        Assert.Contains(new KeyValuePair<EventType, Type>(new EventType("SomeNestedEvent"), typeof(TestDomainEvents.V0.SomeNestedEvent)), options.EventTypes);
        Assert.Contains(new KeyValuePair<EventType, Type>(new EventType("event-with-default-ver"), typeof(SomeEventWithDefaultVersion)), options.EventTypes);
        Assert.Contains(new KeyValuePair<EventType, Type>(new EventType("SomeEvent@1"), typeof(SomeEventV1)), options.EventTypes);
        Assert.Contains(new KeyValuePair<EventType, Type>(new EventType("SomeEvent@202602"), typeof(SomeEventV2026_02)), options.EventTypes);
    }
}
