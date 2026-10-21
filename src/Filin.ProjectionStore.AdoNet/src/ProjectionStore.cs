using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using Filin.AdoNet;
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
[DebuggerDisplay("Invariant = {options.Value.Invariant}")]
sealed class ProjectionStore(IOptions<ProjectionStoreAdoNetOptions> options, ILoggerFactory loggerFactory) : IProjectionStore
{
    readonly DbDataSource _dataSource = options.Value.DataSource;
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.ProjectionStore.AdoNet");
    readonly IStorageSerializer _serializer = options.Value.StorageSerializer;
    readonly ConcurrentDictionary<int, string> _upsertBatchQueries = new();

    /// <summary>
    /// The queries currently used. When this is updated, the new queries will take effect immediately.
    /// </summary>
    StorageQueries? _currentOperationalQueries;

    Ordinals? _ordinals;

    /// <inheritdoc/>
    public async Task<Projection?> LoadAsync(ProjectionId projectionId, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateReading(projectionId);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = opQueries.ReadStorageKey;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

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

                var position = new EventPosition((ulong)reader.GetInt64(ordinals.Position));
                var timestamp = new DateTime(reader.GetDateTime(ordinals.TimestampUtc).Ticks, DateTimeKind.Utc);

                if (reader.TryGetPayload(ordinal: ordinals.Payload, payloadType: payloadType, serializer: _serializer, payload: out var payload))
                {
                    projection = new Projection
                    {
                        Id = projectionId,
                        Position = position,
                        Timestamp = timestamp,
                        Payload = payload,
                    };
                }
            }

            if (projection is null)
            {
                _logger.StateNotFound(projectionId);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.StateRead(projectionId);
            }

            return projection;
        }
        catch (Exception ex) when (ex is not ProjectionStoreException)
        {
            _logger.StateReadError(ex, projectionId);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Projection projection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projection);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateWriting(projection.Id, projection.Position);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = opQueries.UpsertStorageKey;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            var projectionId = projection.Id;

            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
            command.AddParameter(CommandParam.Position, (long)projection.Position.Value);
            command.AddParameter(CommandParam.TimestampUtc, projection.Timestamp.ToUniversalTime());
            command.AddParameter(CommandParam.Payload, _serializer.Serialize(projection.Payload).ToArray());

            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.StateWriteError(ex, projection.Id, projection.Position);

            throw;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateWrote(projection.Id, projection.Position);
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(IEnumerable<Projection> projections, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projections);

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
        var count = Enumerable.TryGetNonEnumeratedCount(projections, out var cnt) ? cnt : projections.Count();
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateBatchWriting(count);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            var commandText = GetUpsertBatchQuery(count);

            using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = commandText;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            var i = 0;
#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
            foreach (var projection in projections)
            {
                var projectionId = projection.Id;

                command.AddParameter(BatchCommandParams.IdHash(i), (long)projectionId.Key.Hash);
                command.AddParameter(BatchCommandParams.IdName(i), projectionId.Key.Value);
                command.AddParameter(BatchCommandParams.TypeHash(i), (long)projectionId.ProjectorId.Hash);
                command.AddParameter(BatchCommandParams.TypeName(i), projectionId.ProjectorId.Value);
                command.AddParameter(BatchCommandParams.Position(i), (long)projection.Position.Value);
                command.AddParameter(BatchCommandParams.TimestampUtc(i), projection.Timestamp.ToUniversalTime());
                command.AddParameter(BatchCommandParams.Payload(i), _serializer.Serialize(projection.Payload).ToArray());

                i++;
            }
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.StateBatchWriteError(ex);

            throw;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateBatchWrote();
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(ProjectionId projectionId, EventPosition position, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateClearing(projectionId, position);
        }

        try
        {
            var opQueries = Interlocked.CompareExchange(ref _currentOperationalQueries, null, null) ?? await InitQueriesAsync(cancellationToken).ConfigureAwait(false);

            using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            using var command = conn.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = opQueries.ClearStorageKey;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            command.AddParameter(CommandParam.IdHash, (long)projectionId.Key.Hash);
            command.AddParameter(CommandParam.IdName, projectionId.Key.Value);
            command.AddParameter(CommandParam.TypeHash, (long)projectionId.ProjectorId.Hash);
            command.AddParameter(CommandParam.TypeName, projectionId.ProjectorId.Value);
            command.AddParameter(CommandParam.Position, (long)position.Value);

            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.StateClearingError(ex, projectionId, position);

            throw;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.StateCleared(projectionId, position);
        }
    }

    async Task<StorageQueries> InitQueriesAsync(CancellationToken cancellationToken)
    {
        const string readStorageKey = nameof(StorageQueries.ReadStorageKey);
        const string upsertStorageKey = nameof(StorageQueries.UpsertStorageKey);
        const string upsertBatchStorageKey = nameof(StorageQueries.UpsertBatchStorageKey);
        const string clearStorageKey = nameof(StorageQueries.ClearStorageKey);

        const string initializationQuery = $"SELECT query_key, query_text FROM Filin_query WHERE query_key IN ('{readStorageKey}','{upsertStorageKey}','{upsertBatchStorageKey}','{clearStorageKey}')";

        using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        using var command = conn.CreateCommand();

        command.CommandText = initializationQuery;

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);

        var queries = new List<(string, string)>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            queries.Add((reader.GetValue<string>("query_key"), reader.GetValue<string>("query_text")));
        }

        var opQueries = new StorageQueries
        {
            ReadStorageKey = queries.Single(i => i.Item1 == readStorageKey).Item2,
            UpsertStorageKey = queries.Single(i => i.Item1 == upsertStorageKey).Item2,
            UpsertBatchStorageKey = queries.Single(i => i.Item1 == upsertBatchStorageKey).Item2,
            ClearStorageKey = queries.Single(i => i.Item1 == clearStorageKey).Item2
        };

        var oldValue = Interlocked.CompareExchange(ref _currentOperationalQueries, opQueries, null);

        if (oldValue is not null)
        {
            return oldValue;
        }

        _logger.StorageInitialized();

        return opQueries;
    }

    Type GetPayloadType(ProjectionId projectionId)
    {
        if (!options.Value.ProjectionTypes.TryGetValue(projectionId.ProjectorId, out var type))
        {
            var ex = new ProjectionStoreException("Unknown runtime type for projectorId.");
            ex.Data[nameof(ProjectionId.ProjectorId)] = projectionId.ProjectorId;

            _logger.NoDeserializer(ex, projectionId);

            throw ex;
        }

        return type;
    }

    string GetUpsertBatchQuery(int paramsCount)
    {
        string CreateUpsertBatchQuery(int paramsCount)
        {
            var queries = Volatile.Read(ref _currentOperationalQueries) ?? throw new InvalidOperationException($"{nameof(_currentOperationalQueries)} is null");

            return queries.UpsertBatchStorageKey
                .Replace(CommandParam.IdHash, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.IdHash)), StringComparison.Ordinal)
                .Replace(CommandParam.IdName, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.IdName)), StringComparison.Ordinal)
                .Replace(CommandParam.TypeHash, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.TypeHash)), StringComparison.Ordinal)
                .Replace(CommandParam.TypeName, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.TypeName)), StringComparison.Ordinal)
                .Replace(CommandParam.Position, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.Position)), StringComparison.Ordinal)
                .Replace(CommandParam.TimestampUtc, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.TimestampUtc)), StringComparison.Ordinal)
                .Replace(CommandParam.Payload, string.Join(',', Enumerable.Range(0, paramsCount).Select(BatchCommandParams.Payload)), StringComparison.Ordinal);
        }

        return _upsertBatchQueries.GetOrAdd(paramsCount, CreateUpsertBatchQuery);
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

    static class BatchCommandParams
    {
        internal static string IdHash(int index) => "@ih" + index;
        internal static string IdName(int index) => "@in" + index;
        internal static string TypeHash(int index) => "@th" + index;
        internal static string TypeName(int index) => "@tn" + index;
        internal static string Position(int index) => "@p" + index;
        internal static string TimestampUtc(int index) => "@t" + index;
        internal static string Payload(int index) => "@pl" + index;
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
