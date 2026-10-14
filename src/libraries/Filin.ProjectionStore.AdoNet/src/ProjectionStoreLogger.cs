using Microsoft.Extensions.Logging;

namespace Filin.ProjectionStore.AdoNet;

static partial class ProjectionStoreLogger
{
    [LoggerMessage(LogLevel.Information,
        EventId = (int)ProjectionStoreCode.ProjectionInitStore,
        EventName = "StorageInitialized",
        Message = "Initialized storage provider: ProviderName={ProviderName} Invariant={InvariantName} ConnectionString={ConnectionString}.")]
    internal static partial void RelationalProviderInitProvider(this ILogger logger, string ProviderName, string InvariantName, string ConnectionString);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionReading,
        EventName = "StateReading",
        SkipEnabledCheck = true,
        Message = "Reading state: ProviderName={ProviderName} ProjectionId={ProjectionId}.")]
    internal static partial void RelationalProviderReading(this ILogger logger, string ProviderName, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Information,
        EventId = (int)ProjectionStoreCode.ProjectionNoStateFound,
        EventName = "StateNotFound",
        Message = "No state found: ProviderName={ProviderName} ProjectionId={ProjectionId}.")]
    internal static partial void RelationalProviderNoStateFound(this ILogger logger, string ProviderName, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionRead,
        EventName = "StateRead",
        SkipEnabledCheck = true,
        Message = "Read state: ProviderName={ProviderName} ProjectionId={ProjectionId}.")]
    internal static partial void RelationalProviderRead(this ILogger logger, string ProviderName, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.ProjectionReadError,
        EventName = "StateReadError",
        Message = "Error reading state: ProviderName={ProviderName} ProjectionId={ProjectionId}.")]
    internal static partial void RelationalProviderReadError(this ILogger logger, Exception Exception, string ProviderName, ProjectionId ProjectionId);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionWriting,
        EventName = "StateWriting",
        SkipEnabledCheck = true,
        Message = "Writing state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderWriting(this ILogger logger, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionBatchWriting,
        EventName = "StateBatchWriting",
        SkipEnabledCheck = true,
        Message = "Writing state: ProviderName={ProviderName} ProjectionsCount={ProjectionsCount}.")]
    internal static partial void RelationalProviderBatchWriting(this ILogger logger, string ProviderName, int ProjectionsCount);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.ProjectionWriteError,
        EventName = "StateWriteError",
        Message = "Error writing state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderWriteError(this ILogger logger, Exception Exception, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.ProjectionBatchWriteError,
        EventName = "ProjectionBatchWriteError",
        Message = "Error writing state: ProviderName={ProviderName}.")]
    internal static partial void RelationalProviderBatchWriteError(this ILogger logger, Exception Exception, string ProviderName);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionWrote,
        EventName = "StateWrote",
        SkipEnabledCheck = true,
        Message = "Wrote state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderWrote(this ILogger logger, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionBatchWrote,
        EventName = "StateBatchWrote",
        SkipEnabledCheck = true,
        Message = "Wrote state: ProviderName={ProviderName}.")]
    internal static partial void RelationalProviderBatchWrote(this ILogger logger, string ProviderName);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionClearing,
        EventName = "StateClearing",
        SkipEnabledCheck = true,
        Message = "Clearing state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderClearing(this ILogger logger, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.ProjectionClearingError,
        EventName = "StateClearingError",
        Message = "Error clearing state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderDeleteError(this ILogger logger, Exception Exception, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionCleared,
        EventName = "StateCleared",
        SkipEnabledCheck = true,
        Message = "Cleared state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderCleared(this ILogger logger, string ProviderName, ProjectionId ProjectionId, EventPosition Position);
}
