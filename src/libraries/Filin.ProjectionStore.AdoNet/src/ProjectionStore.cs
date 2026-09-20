using System.Data;
using System.Data.Common;
using System.Diagnostics;

using Filin.AdoNet;
using Filin.Compliance.Classification;
using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.ProjectionStore.AdoNet;

/// <summary>
/// A storage provider for writing state data to relational storage.
/// </summary>
/// <remarks>
/// </remarks>
[DebuggerDisplay("Name = {name}, ConnectionName = {options.Value.ConnectionName}")]
sealed class ProjectionStore(
    DbDataSource dataSource,
    ILoggerFactory loggerFactory,
    IOptions<ProjectionStoreAdoNetOptions> options,
    string name) : IProjectionStore
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.ProjectionStore.AdoNet");
    readonly IStorageSerializer _serializer = options.Value.StorageSerializer;

    /// <summary>
    /// The queries currently used. When this is updated, the new queries will take effect immediately.
    /// </summary>
    RelationalStorageProviderQueries? _currentOperationalQueries;

    Ordinals? _ordinals;

    /// <inheritdoc/>
    public async Task<Projection?> LoadAsync(ProjectionId projectionId, CancellationToken cancellationToken = default)
    {
        //It assumed these parameters are always valid. If not, an exception will be thrown, even if not as clear
        //as with explicitly checked parameters.
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.RelationalProviderReading(name, projectionId);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

            command.CommandText = opQueries.ReadStorageKey;

            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);

            Projection? projection = null;

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var payloadType = GetPayloadType(projectionId);

                var ordinals = _ordinals ??= Ordinals.CreateOrdinals(reader);

                if (reader.TryGetPayload(ordinal: ordinals.Payload, payloadType: payloadType, serializer: _serializer, payload: out var payload))
                {
                    projection = new Projection
                    {
                        Id = projectionId,
                        Position = new EventPosition((ulong)reader.GetInt64(ordinals.Position)),
                        Timestamp = new DateTime(reader.GetDateTime(ordinals.TimestampUtc).Ticks, DateTimeKind.Utc),
                        Payload = payload,
                    };
                }
            }

            if (projection is null)
            {
                _logger.RelationalProviderNoStateFound(name, projectionId);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.RelationalProviderRead(name, projectionId);
            }

            return projection;
        }
        catch (Exception ex)
        {
            _logger.RelationalProviderReadError(ex, name, projectionId);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Projection projection, bool create, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        //It assumed these parameters are always valid. If not, an exception will be thrown, even if not as clear
        //as with explicitly checked parameters.
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.RelationalProviderWriting(name, projection.Id, projection.Position);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            var query = create ? opQueries.CreateStorageKey : opQueries.UpdateStorageKey;

            using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

            command.CommandText = query;

            var projectionId = projection.Id;

            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
            command.AddParameter(CommandParam.Position, (long)projection.Position.Value);
            command.AddParameter(CommandParam.TimestampUtc, projection.Timestamp.ToUniversalTime());
            command.AddParameter(CommandParam.Payload, _serializer.Serialize(projection.Payload).ToArray());

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.RelationalProviderWriteError(ex, name, projection.Id, projection.Position);

            throw;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.RelationalProviderWrote(name, projection.Id, projection.Position);
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(ProjectionId projectionId, EventPosition position, CancellationToken cancellationToken = default)
    {
        //It assumed these parameters are always valid. If not, an exception will be thrown, even if not as clear
        //as with explicitly checked parameters.
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.RelationalProviderClearing(name, projectionId, position);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

            command.CommandText = opQueries.ClearStorageKey;

            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
            command.AddParameter(CommandParam.Position, (long)position.Value);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.RelationalProviderDeleteError(ex, name, projectionId, position);

            throw;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.RelationalProviderCleared(name, projectionId, position);
        }
    }

    async Task<RelationalStorageProviderQueries> InitQueriesAsync(CancellationToken cancellationToken)
    {
        const string CreateStorageKey = nameof(RelationalStorageProviderQueries.CreateStorageKey);
        const string ReadStorageKey = nameof(RelationalStorageProviderQueries.ReadStorageKey);
        const string UpdateStorageKey = nameof(RelationalStorageProviderQueries.UpdateStorageKey);
        const string ClearStorageKey = nameof(RelationalStorageProviderQueries.ClearStorageKey);

        const string InitializationQuery = $"SELECT query_key, query_text FROM Filin_query WHERE query_key = '{CreateStorageKey}' OR query_key = '{ReadStorageKey}' OR query_key = '{UpdateStorageKey}' OR query_key = '{ClearStorageKey}'";

        using var conn = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        using var command = conn.CreateCommand();

        command.CommandText = InitializationQuery;

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

        var queries = new List<(string, string)>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            queries.Add((reader.GetValue<string>("query_key"), reader.GetValue<string>("query_text")));
        }

        var opQueries = new RelationalStorageProviderQueries
        {
            CreateStorageKey = queries.Single(i => i.Item1 == CreateStorageKey).Item2,
            ReadStorageKey = queries.Single(i => i.Item1 == ReadStorageKey).Item2,
            UpdateStorageKey = queries.Single(i => i.Item1 == UpdateStorageKey).Item2,
            ClearStorageKey = queries.Single(i => i.Item1 == ClearStorageKey).Item2
        };

        var oldValue = Interlocked.CompareExchange(ref _currentOperationalQueries, opQueries, null);

        if (oldValue is not null)
        {
            return oldValue;
        }

        _logger.RelationalProviderInitProvider(name, dataSource.GetType().FullName ?? string.Empty, dataSource.ConnectionString);

        return opQueries;
    }

    Type GetPayloadType(ProjectionId projectionId)
    {
        if (!options.Value.ProjectionTypes.TryGetValue(projectionId.ProjectorId, out var type))
        {
            var ex = new DcbEventStoreException("Unknown runtime type for ProjectorId.");
            ex.Data[nameof(ProjectionId.ProjectorId)] = projectionId.ProjectorId;

            _logger.RelationalProviderReadError(Exception: ex, ProviderName: name, ProjectionId: projectionId);

            throw ex;
        }

        return type;
    }

    static class CommandParam
    {
        internal const string IdHash = "@id_hash";
        internal const string IdName = "@id_name";
        internal const string TypeHash = "@type_hash";
        internal const string TypeName = "@type_name";
        internal const string Position = "@position";
        internal const string TimestampUtc = "@timestamp_utc";
        internal const string Payload = "@payload";
    }

    sealed record Ordinals
    {
        internal int Position { get; init; }
        internal int TimestampUtc { get; init; }
        internal int Payload { get; init; }

        internal static Ordinals CreateOrdinals(DbDataReader dataReader) => new()
        {
            Position = dataReader.GetOrdinal("position"),
            TimestampUtc = dataReader.GetOrdinal("timestamp_utc"),
            Payload = dataReader.GetOrdinal("payload")
        };
    }
}

static partial class AdoNetProjectionStoreLogger
{
    [LoggerMessage(LogLevel.Information,
        EventId = (int)ProjectionStoreCode.ProjectionInitStore,
        EventName = "StorageInitialized",
        Message = "Initialized storage provider: ProviderName={ProviderName} Invariant={InvariantName} ConnectionString={ConnectionString}.")]
    internal static partial void RelationalProviderInitProvider(this ILogger logger, string ProviderName, string InvariantName, [ConnectionString] string ConnectionString);

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

    [LoggerMessage(LogLevel.Error,
        EventId = (int)ProjectionStoreCode.ProjectionWriteError,
        EventName = "StateWriteError",
        Message = "Error writing state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderWriteError(this ILogger logger, Exception Exception, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

    [LoggerMessage(LogLevel.Trace,
        EventId = (int)ProjectionStoreCode.ProjectionWrote,
        EventName = "StateWrote",
        SkipEnabledCheck = true,
        Message = "Wrote state: ProviderName={ProviderName} ProjectionId={ProjectionId} Position={Position}.")]
    internal static partial void RelationalProviderWrote(this ILogger logger, string ProviderName, ProjectionId ProjectionId, EventPosition Position);

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
