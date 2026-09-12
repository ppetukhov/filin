using System.Data;
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
/// <para>
/// Required configuration params: <c>DataConnectionString</c>
/// </para>
/// </remarks>
[DebuggerDisplay("Name = {name}, ConnectionString = {_storage.ConnectionString}")]
public sealed class ProjectionStore(
    ILoggerFactory loggerFactory,
    IOptions<ProjectionStoreAdoNetOptions> options,
    string name) : IProjectionStore
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.ProjectionStore.AdoNet");
    readonly IStorageSerializer _serializer = options.Value.StorageSerializer;

    /// <summary>
    /// The storage used for back-end operations.
    /// </summary>
    readonly IRelationalStorage _storage = RelationalStorage.CreateInstance(options.Value.Invariant, options.Value.ConnectionString);

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

            var resultRecords = await _storage.ReadAsync(opQueries.ReadStorageKey, parameterProvider: command =>
                    {
                        command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
                        command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
                        command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
                        command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
                    },
                    selector: (selector, resultSetCount, token) =>
                    {
                        var payloadType = GetPayloadType(projectionId);

                        var ordinals = _ordinals ??= Ordinals.CreateOrdinals(selector);

                        if (selector.TryGetPayload(ordinal: ordinals.Payload, payloadType: payloadType, serializer: _serializer, payload: out var payload))
                        {
                            var projection = new Projection
                            {
                                Id = projectionId,
                                Position = new EventPosition((ulong)selector.GetInt64(ordinals.Position)),
                                Timestamp = new DateTime(selector.GetDateTime(ordinals.TimestampUtc).Ticks, DateTimeKind.Utc),
                                Payload = payload,
                            };

                            return Task.FromResult(projection);
                        }

                        return Task.FromResult((Projection)null!);
                    },
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

            var projection = resultRecords.SingleOrDefault();

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

            _ = await _storage.ExecuteAsync(query, command =>
                        {
                            var projectionId = projection.Id;

                            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
                            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
                            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
                            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
                            command.AddParameter(CommandParam.Position, (long)projection.Position.Value);
                            command.AddParameter(CommandParam.TimestampUtc, projection.Timestamp.ToUniversalTime());
                            command.AddParameter(CommandParam.Payload, _serializer.Serialize(projection.Payload).ToArray());
                        },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
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

            _ = await _storage.ExecuteAsync(opQueries.ClearStorageKey, command =>
                        {
                            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
                            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
                            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
                            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
                            command.AddParameter(CommandParam.Position, (long)position.Value);
                        },
                        cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);
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

        var queries = await _storage.ReadAsync(InitializationQuery, static command => { }, static (selector, resultSetCount, token) =>
                Task.FromResult((selector.GetValue<string>("query_key"), selector.GetValue<string>("query_text"))),
            cancellationToken: cancellationToken).ConfigureAwait(false);

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

        _logger.RelationalProviderInitProvider(name, _storage.InvariantName, _storage.ConnectionString);

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

        internal static Ordinals CreateOrdinals(IDataRecord dataRecord) => new()
        {
            Position = dataRecord.GetOrdinal("position"),
            TimestampUtc = dataRecord.GetOrdinal("timestamp_utc"),
            Payload = dataRecord.GetOrdinal("payload")
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
