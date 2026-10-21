namespace Filin.EventStore.AdoNet.Internal;

struct EventStreamMessage : IComparable<EventStreamMessage>
{
    internal DcbEventSequenced EventSequenced;
    internal StreamCommand? Command;

    /// <inheritdoc/>
    public readonly int CompareTo(EventStreamMessage other) =>
        Command is not null && other.Command is not null ? 0 : Command is not null ? -1 : other.Command is not null ? 1 : 0;

    internal static EventStreamMessage Event(DcbEventSequenced eventSequenced) => new() { EventSequenced = eventSequenced };

    internal static EventStreamMessage Subscribe(IDcbEventStream enumerator) => new() { Command = new SubscribeCommand(new WeakReference<IDcbEventStream>(enumerator)) };

    internal static EventStreamMessage Unsubscribe(IDcbEventStream enumerator) => new() { Command = new UnsubscribeCommand(new WeakReference<IDcbEventStream>(enumerator)) };
}
