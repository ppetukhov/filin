using Filin.Abstractions.Tests;
using Filin.Abstractions.Tests.Configuration;

namespace Filin.Abstractions.EventTypeTests;

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

        evt.GetEventType().Should().Be(new EventType("SomeEvent"));
    }

    [Fact]
    public void SomeNestedEventGetEventTypeReturnSomeNestedEvent()
    {
        var evt = new TestDomainEvents.V0.SomeNestedEvent();

        evt.GetEventType().Should().Be(new EventType("SomeNestedEvent"));
    }

    [Fact]
    public void SomeEventWithDefaultVersionGetEventTypeReturnWithWithOutVersion()
    {
        var evt = new SomeEventWithDefaultVersion();

        evt.GetEventType().Should().Be(new EventType("event-with-default-ver"));
    }

    [Fact]
    public void SomeEventV1GetEventTypeReturnSomeEventAt1()
    {
        var evt = new SomeEventV1();

        evt.GetEventType().Should().Be(new EventType("SomeEvent@1"));
    }

    [Fact]
    public void SomeEventV2026_02GetEventTypeReturnSomeEventAt202602()
    {
        var evt = new SomeEventV2026_02();

        evt.GetEventType().Should().Be(new EventType("SomeEvent@202602"));
    }

    [Fact]
    public void MetadataEventTypes_Configure_EventTypes()
    {
        var options = new Configuration.TypeManifestOptions();
        var provider = new FilinAbstractionsTestsEventTypesTypeManifestProvider();

        provider.Configure(options);

        options.EventTypes.Should().HaveCount(5);

        options.EventTypes.Should().ContainEquivalentOf<KeyValuePair<EventType, Type>>(new(new EventType("SomeEvent"), typeof(SomeEvent)));
        options.EventTypes.Should().ContainEquivalentOf<KeyValuePair<EventType, Type>>(new(new EventType("SomeNestedEvent"), typeof(TestDomainEvents.V0.SomeNestedEvent)));
        options.EventTypes.Should().ContainEquivalentOf<KeyValuePair<EventType, Type>>(new(new EventType("event-with-default-ver"), typeof(SomeEventWithDefaultVersion)));
        options.EventTypes.Should().ContainEquivalentOf<KeyValuePair<EventType, Type>>(new(new EventType("SomeEvent@1"), typeof(SomeEventV1)));
        options.EventTypes.Should().ContainEquivalentOf<KeyValuePair<EventType, Type>>(new(new EventType("SomeEvent@202602"), typeof(SomeEventV2026_02)));
    }
}
