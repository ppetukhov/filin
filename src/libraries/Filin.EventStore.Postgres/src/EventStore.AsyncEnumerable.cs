using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

using Filin.AdoNet;
using Filin.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres;

sealed partial class EventStore
{
    /// <inheritdoc/>
    sealed class AsyncEnumerable(
            DcbQuery query,
            EventPosition afterPosition,
            IOptions<EventStorePostgresOptions> options,
            ILogger logger) : IAsyncEnumerable<DcbEventSequenced>
    {
        /// <inheritdoc/>
        public IAsyncEnumerator<DcbEventSequenced> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var opt = options.Value;

#pragma warning disable CA2000 // Dispose objects before losing scope
            var command = CreateReadEventsCommand(CreateConnection(opt), afterPosition, query, opt.BatchSize);
#pragma warning restore CA2000 // Dispose objects before losing scope

            return new AsyncEnumerator(command, InitialEnumeratorEvent(afterPosition), logger, opt, cancellationToken);
        }
    }

    /// <inheritdoc/>
    sealed class AsyncEnumerator(
            DbCommand command,
            DcbEventSequenced initialEvent,
            ILogger logger,
            EventStorePostgresOptions options,
            CancellationToken cancellationToken
        ) : IAsyncEnumerator<DcbEventSequenced>
    {
        bool _disposed;
        Ordinals? _ordinals;
        DbDataReader? _reader;
        long _readCount;

        /// <inheritdoc/>
        public DcbEventSequenced Current { get; private set; } = initialEvent;

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (cancellationToken.IsCancellationRequested)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.ReadingEventsCancelled(_readCount);
                    }

                    return false;
                }

                var reader = await GetReaderAsync().ConfigureAwait(false);

                EventPosition position;
                Ordinals ordinals;

                while (true)
                {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (_readCount > 0 && _readCount % options.BatchSize == 0)
                        {
                            await reader.DisposeAsync().ConfigureAwait(false);

                            _reader = null;

                            var afterParam = command.Parameters[COMMAND_PARAMETER_NAME_AFTER];

                            afterParam.Value = Current.Position.Value;

                            reader = await GetReaderAsync().ConfigureAwait(false);

                            continue;
                        }

                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.EventsReaded(_readCount);
                        }

                        return false;
                    }

                    _ = Interlocked.Increment(ref _readCount);

                    ordinals = _ordinals ??= Ordinals.CreateOrdinals(reader);

                    position = new EventPosition((uint)reader.GetInt64(ordinals.Position));

                    Debug.Assert(position > Current.Position);

                    break;
                }

                var timestamp = new DateTime(reader.GetDateTime(ordinals.TimestampUtc).Ticks, DateTimeKind.Utc);
                var eventType = (EventType)GetIdentity(ordinalHash: ordinals.TypeHash, ordinalValue: ordinals.TypeName, reader: reader);
                var tags = GetTags(reader, ordinals);

                if (!options.EventTypes.TryGetValue(eventType, out var payloadType))
                {
                    var ex = new DcbEventStoreException("Unknown runtime type for EventType.");
                    ex.Data[nameof(DcbEvent.EventType)] = eventType;

                    logger.ErrorReadingEvents(exception: ex);

                    throw ex;
                }

                if (!reader.TryGetPayload(ordinal: ordinals.Payload, payloadType: payloadType, serializer: options.StorageSerializer, payload: out var data))
                {
                    var ex = new DcbEventStoreException("Corrupted event. Event data is null.");
                    ex.Data[nameof(DcbEvent.EventType)] = eventType;
                    ex.Data[nameof(DcbEvent.Data)] = null;
                    ex.Data[nameof(DcbEvent.Timestamp)] = timestamp;
                    ex.Data[nameof(DcbEventSequenced.Position)] = position;

                    logger.ErrorReadingEvents(exception: ex);

                    throw ex;
                }

                var dcbEvent = new DcbEvent { EventType = eventType, Tags = tags, Timestamp = timestamp, Data = data };

                Current = new DcbEventSequenced(position, dcbEvent);
            }
            catch (OperationCanceledException)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.ReadingEventsCancelled(_readCount);
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.ErrorReadingEvents(ex);

                throw EventStoreException(ex);
            }

            return true;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.SuppressFinalize(this);

            if (_reader is not null)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    await _reader.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            var conn = command.Connection;

            // disposing order: command then connection
            command.Dispose();

            if (conn is not null)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    await conn.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        static Identity GetIdentity(int ordinalHash, int ordinalValue, DbDataReader reader)
        {
            var bytes = reader.GetBytes(ordinal: ordinalValue, dataOffset: 0, buffer: null, bufferOffset: 0, length: 0);

            if (bytes > 0)
            {
                var value = new byte[bytes];

                var readed = reader.GetBytes(ordinal: ordinalValue, dataOffset: 0, buffer: value, bufferOffset: 0, length: (int)bytes);

                Debug.Assert(readed == bytes);

                return Identity.UnsafeCreate(value, hashCode: reader.GetInt32(ordinalHash));
            }

            return default;
        }

        static ImmutableHashSet<Tag>? GetTags(DbDataReader reader, Ordinals ordinals)
        {
            var tag_values = reader.GetFieldValue<string[]>(ordinals.TagNames);

            if (tag_values.Length == 0)
            {
                return null;
            }

            var tag_hashes = reader.GetFieldValue<int[]>(ordinals.TagHashes);

            var hashSet = ImmutableHashSet.CreateBuilder<Tag>();

            for (int i = 0; i < tag_values.Length; i++)
            {
                var value = Encoding.UTF8.GetBytes(tag_values[i]);

                var tag = Tag.FromIdentity(Identity.UnsafeCreate(value: value, hashCode: tag_hashes[i]));

                hashSet.Add(tag);
            }

            return hashSet.ToImmutable();
        }

        async ValueTask<DbDataReader> GetReaderAsync()
        {
            var reader = _reader;

            if (reader is null || reader.IsClosed)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.ReadingEvents();
                }

                if (command.Connection is { State: ConnectionState.Closed } conn)
                {
                    await conn.ConnectToEventStoreAsync(options.Backoff, logger, cancellationToken).ConfigureAwait(false);
                }

                reader = _reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            }

            return reader;
        }
    }

    sealed record Ordinals
    {
        internal int Position { get; init; }
        internal int TimestampUtc { get; init; }
        internal int TypeHash { get; init; }
        internal int TypeName { get; init; }
        internal int TagHashes { get; init; }
        internal int TagNames { get; init; }
        internal int Payload { get; init; }

        internal static Ordinals CreateOrdinals(DbDataReader reader) => new()
        {
            Position = reader.GetOrdinal(ColumnNames.Position),
            TimestampUtc = reader.GetOrdinal(ColumnNames.TimestampUtc),
            TypeHash = reader.GetOrdinal(ColumnNames.TypeHash),
            TypeName = reader.GetOrdinal(ColumnNames.TypeName),
            TagHashes = reader.GetOrdinal(ColumnNames.TagHashes),
            TagNames = reader.GetOrdinal(ColumnNames.TagNames),
            Payload = reader.GetOrdinal(ColumnNames.Payload)
        };
    }
}
