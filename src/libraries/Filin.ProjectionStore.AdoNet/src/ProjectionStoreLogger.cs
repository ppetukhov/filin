using Microsoft.Extensions.Logging;

namespace Filin.ProjectionStore.AdoNet;

static partial class ProjectionStoreLogger
{
    [LoggerMessage(LogLevel.Information,
        EventId = (int)ProjectionStoreCode.StorageInitialized,
        EventName = "StorageInitialized",
        Message = "Initialized storage provider.")]
    internal static partial void StorageInitialized(this ILogger logger);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateReading,
        EventName = "StateReading",
        SkipEnabledCheck = true,
        Message = "Reading state: ProjectionId={ProjectionId}.")]
    internal static partial void StateReading(this ILogger logger, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)ProjectionStoreCode.StateNotFound,
        EventName = "StateNotFound",
        Message = "No state found: ProjectionId={ProjectionId}.")]
    internal static partial void StateNotFound(this ILogger logger, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateRead,
        EventName = "StateRead",
        SkipEnabledCheck = true,
        Message = "Read state: ProjectionId={ProjectionId}.")]
    internal static partial void StateRead(this ILogger logger, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.NoDeserializer,
        EventName = "NoDeserializer",
        Message = "No deserializer found: ProjectionId={ProjectionId}.")]
    internal static partial void NoDeserializer(this ILogger logger, Exception Exception, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.StateReadError,
        EventName = "StateReadError",
        Message = "Error reading state: ProjectionId={ProjectionId}.")]
    internal static partial void StateReadError(this ILogger logger, Exception Exception, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateWriting,
        EventName = "StateWriting",
        SkipEnabledCheck = true,
        Message = "Writing state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateWriting(this ILogger logger, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateBatchWriting,
        EventName = "StateBatchWriting",
        SkipEnabledCheck = true,
        Message = "Writing state: ProjectionsCount={ProjectionsCount}.")]
    internal static partial void StateBatchWriting(this ILogger logger, int ProjectionsCount);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.StateWriteError,
        EventName = "StateWriteError",
        Message = "Error writing state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateWriteError(this ILogger logger, Exception Exception, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.StateBatchWriteError,
        EventName = "StateBatchWriteError",
        Message = "Error writing state.")]
    internal static partial void StateBatchWriteError(this ILogger logger, Exception Exception);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateWrote,
        EventName = "StateWrote",
        SkipEnabledCheck = true,
        Message = "Wrote state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateWrote(this ILogger logger, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateBatchWrote,
        EventName = "StateBatchWrote",
        SkipEnabledCheck = true,
        Message = "Wrote state.")]
    internal static partial void StateBatchWrote(this ILogger logger);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateClearing,
        EventName = "StateClearing",
        SkipEnabledCheck = true,
        Message = "Clearing state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateClearing(this ILogger logger, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.StateClearingError,
        EventName = "StateClearingError",
        Message = "Error clearing state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateClearingError(this ILogger logger, Exception Exception, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.StateCleared,
        EventName = "StateCleared",
        SkipEnabledCheck = true,
        Message = "Cleared state: ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void StateCleared(this ILogger logger, ProjectionId ProjectionId, EventPosition Position);
}
