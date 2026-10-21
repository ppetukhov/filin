using System.Data;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Filin.EventStore.AdoNet;

/// <summary>
/// Provides highly optimized, source-generated logging extensions for the EventStore and streaming services.
/// </summary>
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Performance")]
static partial class Logger
{
    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsReading,
        EventName = "EventsReading",
        SkipEnabledCheck = true,
        Message = "Reading events from EventStore")]
    internal static partial void ReadingEvents(this ILogger logger);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsReaded,
        EventName = "EventsReaded",
        SkipEnabledCheck = true,
        Message = "Events were read from EventStore. EventsCount: {EventsCount}")]
    internal static partial void EventsReaded(this ILogger logger, long EventsCount);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsReadingCancelled,
        EventName = "EventsReadingCancelled",
        SkipEnabledCheck = true,
        Message = "Reading events from EventStore was canceled. EventsCount: {EventsCount}")]
    internal static partial void ReadingEventsCancelled(this ILogger logger, long EventsCount);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.EventsReadError,
        EventName = "EventsReadError",
        Message = "Error occurred while reading events from EventStore")]
    internal static partial void ErrorReadingEvents(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsAppending,
        EventName = "EventsAppending",
        SkipEnabledCheck = true,
        Message = "Appending events to EventStore")]
    internal static partial void AppendingEvents(this ILogger logger);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)EventStoreCode.EventsAppendingConflict,
        EventName = "EventsAppendingConflict",
        Message = "Conflict occurred while appending events to EventStore")]
    internal static partial void ConflictAppendingEvents(this ILogger logger);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsAppended,
        EventName = "EventsAppended",
        SkipEnabledCheck = true,
        Message = "Events successfully appended to EventStore")]
    internal static partial void EventsAppended(this ILogger logger);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventsAppendingCancelled,
        EventName = "EventsAppendingCancelled",
        SkipEnabledCheck = true,
        Message = "Appending events to EventStore has been canceled")]
    internal static partial void AppendingEventsCancelled(this ILogger logger);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.EventsAppendError,
        EventName = "EventsAppendError",
        Message = "Error occurred while appending events to EventStore")]
    internal static partial void ErrorAppendEvents(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventStoreConnectingDelay,
        EventName = "EventStoreConnectingDelay",
        SkipEnabledCheck = true,
        Message = "Delaying for {delay} before connecting to EventStore")]
    internal static partial void EventStoreConnectingDelay(this ILogger logger, TimeSpan Delay);

    [LoggerMessage(LogLevel.Warning,
        EventId = (int)EventStoreCode.EventStoreConnectingRetry,
        EventName = "EventStoreConnectingRetry",
        Message = "Connecting to EventStore. Retry attempt: {Retry}")]
    internal static partial void EventStoreConnectingRetry(this ILogger logger, int Retry);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventStoreConnected,
        EventName = "EventStoreConnected",
        SkipEnabledCheck = true,
        Message = "Successfully connected to EventStore")]
    internal static partial void EventStoreConnected(this ILogger logger);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)EventStoreCode.EventStoreReconnected,
        EventName = "EventStoreReconnected",
        Message = "Successfully reconnected to EventStore")]
    internal static partial void EventStoreReconnected(this ILogger logger);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.EventStoreConnectError,
        EventName = "EventStoreConnectError",
        Message = "Event streaming service failed to connect to the database")]
    internal static partial void EventStoreConnectError(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.EventStoreConnectionStateChanged,
        EventName = "EventStoreConnectionStateChanged",
        SkipEnabledCheck = true,
        Message = "Connection state to EventStore changed from {OriginalState} to {CurrentState}")]
    internal static partial void EventStoreConnectionStateChanged(this ILogger logger, ConnectionState OriginalState, ConnectionState CurrentState);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)EventStoreCode.EventStoreConnecting,
        EventName = "EventStoreConnecting",
        Message = "Connecting to EventStore")]
    internal static partial void EventStoreConnecting(this ILogger logger);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)EventStoreCode.StreamingServiceStarted,
        EventName = "StreamingServiceStarted",
        SkipEnabledCheck = true,
        Message = "Event streaming service started")]
    internal static partial void StreamingServiceStarted(this ILogger logger);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)EventStoreCode.StreamingServiceStopped,
        EventName = "StreamingServiceStopped",
        Message = "Event streaming service stopped")]
    internal static partial void StreamingServiceStopped(this ILogger logger);

    [LoggerMessage(LogLevel.Critical,
        EventId = (int)EventStoreCode.StreamingServiceError,
        EventName = "StreamingServiceError",
        Message = "A critical error occurred in the event streaming service")]
    internal static partial void StreamingServiceError(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug,
        EventId = (int)EventStoreCode.StreamingServiceConnectingDelay,
        EventName = "StreamingServiceConnectingDelay",
        SkipEnabledCheck = true,
        Message = "Event streaming service delaying for {delay} before connecting to the database")]
    internal static partial void StreamingServiceConnectingDelay(this ILogger logger, TimeSpan Delay);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.StreamingServiceListenPostgresEventsDbError,
        EventName = "StreamingServiceListenPostgresEventsDbError",
        Message = "Event streaming service error occurred while listening to Postgres events. SqlState: {SqlState}. ErrorCode: {ErrorCode}")]
    internal static partial void StreamingServiceListenPostgresEventsDbError(this ILogger logger, Exception exception, string? SqlState, int ErrorCode);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.StreamingServiceListenPostgresEventsError,
        EventName = "StreamingServiceListenPostgresEventsError",
        Message = "Event streaming service error occurred while listening to Postgres events")]
    internal static partial void StreamingServiceListenPostgresEventsError(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)EventStoreCode.StreamingServiceEventProcessingError,
        EventName = "StreamingServiceEventProcessingError",
        Message = "Event streaming service error occurred while processing the event")]
    internal static partial void StreamingServiceEventProcessingError(this ILogger logger, Exception exception);
}
