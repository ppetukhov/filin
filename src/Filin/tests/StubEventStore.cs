using Filin.Storage;

namespace Filin.Tests;

public class StubEventStore(IEnumerable<DcbEventSequenced>? events = null) : IEventStore
{
    readonly List<DcbEventSequenced> _events = events?.ToList() ?? [];

    readonly Lock _lock = new();

    public async IAsyncEnumerable<DcbEventSequenced> ReadAsync(
        DcbQuery query,
        EventPosition after)
    {
        await Task.CompletedTask;

        List<DcbEventSequenced> snapshot;

        lock (_lock)
        {
            snapshot = [.. _events];
        }

        foreach (var @event in snapshot)
        {
            if (@event.Position >= after && query.MatchesEvent(@event.Event))
            {
                yield return @event;
            }
        }
    }

    public Task<AppendResult> AppendAsync(
        DcbEvent dcbEvent,
        AppendCondition appendCondition,
        CancellationToken cancellationToken) => AppendAsync([dcbEvent], appendCondition, cancellationToken);

    public async Task<AppendResult> AppendAsync(
        IEnumerable<DcbEvent> dcbEvent,
        AppendCondition appendCondition,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            // 1. Optimistic Concurrency Check:
            // If any event was appended after condition.Position, a conflict has occurred.
            var isConflict = _events.Any(e => e.Position > appendCondition.After && appendCondition.FailIfEventsMatch.MatchesEvent(e.Event));

            if (isConflict)
            {
                // Return conflict result so CommandHandler can trigger a state reload/retry
                return AppendResult.Conflict;
            }

            // 2. Append events to the store with incremented positions:
            var lastPositionValue = _events.Count > 0 ? _events.Last().Position.Value : 0;
            var sequencedEvents = new List<DcbEventSequenced>();

            foreach (var evt in dcbEvent)
            {
                lastPositionValue++;
                var sequencedEvent = new DcbEventSequenced(
                    Position: new EventPosition(lastPositionValue),
                    Timestamp: DateTimeOffset.UtcNow,
                    Event: evt
                );

                _events.Add(sequencedEvent);
                sequencedEvents.Add(sequencedEvent);
            }

            // Return successful append response with new sequenced events
            return new AppendResult(sequencedEvents);
        }
    }

    public IAsyncEnumerable<DcbEventSequenced> Subscribe(
        DcbQuery query,
        EventPosition after) => ReadAsync(query, after);
}
