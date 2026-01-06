using static Filin.EventStore.Postgres.EventStore;

namespace Filin.EventStore.Postgres;

struct Message : IComparable<Message>
{
    internal DcbEventSequenced? EventSequenced;
    internal StreamCommand? Command;

    internal static Message Event(DcbEventSequenced eventSequenced) => new() { EventSequenced = eventSequenced };

    internal static Message Subscribe(StreamAsyncEnumerator enumerator) => new() { Command = new SubscribeCommand(new WeakReference<StreamAsyncEnumerator>(enumerator)) };

    internal static Message Unsubscribe(StreamAsyncEnumerator enumerator) => new() { Command = new UnsubscribeCommand(new WeakReference<StreamAsyncEnumerator>(enumerator)) };

    /// <inheritdoc/>
    public readonly int CompareTo(Message other) =>
        Command is not null && other.Command is not null ? 0 : Command is not null ? -1 : other.Command is not null ? 1 : 0;

    internal abstract record StreamCommand(WeakReference<StreamAsyncEnumerator> Subscriber);

    internal sealed record SubscribeCommand(WeakReference<StreamAsyncEnumerator> Subscriber) : StreamCommand(Subscriber);

    internal sealed record UnsubscribeCommand(WeakReference<StreamAsyncEnumerator> Subscriber) : StreamCommand(Subscriber);
}
