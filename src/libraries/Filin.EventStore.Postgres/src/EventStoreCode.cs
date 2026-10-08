namespace Filin.EventStore.Postgres;

/// <summary>
/// Logging codes used by <see cref="EventStore"/>.
/// </summary>
enum EventStoreCode
{
    EventStoreBase = 100,

    EventsReading = EventStoreBase + 1,
    EventsReaded = EventStoreBase + 2,
    EventsReadError = EventStoreBase + 3,
    EventsReadingCancelled = EventStoreBase + 4,

    EventsAppending = EventStoreBase + 21,
    EventsAppended = EventStoreBase + 22,
    EventsAppendingConflict = EventStoreBase + 23,
    EventsAppendError = EventStoreBase + 24,
    EventsAppendingCancelled = EventStoreBase + 25,

    EventStoreConnecting = EventStoreBase + 10,
    EventStoreConnectingDelay = EventStoreBase + 30,
    EventStoreConnectingRetry = EventStoreBase + 31,
    EventStoreConnected = EventStoreBase + 32,
    EventStoreReconnected = EventStoreBase + 33,
    EventStoreConnectionStateChanged = EventStoreBase + 34,
    EventStoreConnectError = EventStoreBase + 35,

    StreamingBase = EventStoreBase + 100,
    StreamingServiceStarted = StreamingBase + 1,
    StreamingServiceStopped = StreamingBase + 2,
    StreamingServiceError = StreamingBase + 3,

    StreamingServiceConnectingDelay = StreamingBase + 11,

    StreamingServiceListenPostgresEventsDbError = StreamingBase + 21,
    StreamingServiceListenPostgresEventsError = StreamingBase + 22,
    StreamingServiceEventProcessingError = StreamingBase + 23,
}
