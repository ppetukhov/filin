using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

using Filin.AdoNet;
using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Filin.EventStore.Postgres;

/// <summary>
/// Реализация хранилища <see cref="IEventStore"/> в PostgreSQL.
/// </summary>
/// <param name="messageWriter"><inheritdoc cref="ChannelWriter{T}" path="/summary"/></param>
/// <param name="loggerFactory"><inheritdoc cref="ILoggerFactory" path="/summary"/></param>
/// <param name="options"><inheritdoc cref="IOptions{TOptions}" path="/summary"/></param>
sealed partial class EventStore(
    ChannelWriter<Message> messageWriter,
    ILoggerFactory loggerFactory,
    IOptions<EventStorePostgresOptions> options) : IEventStore
{
    const string COMMAND_PARAMETER_NAME_AFTER = "@aft";
    const string COMMAND_PARAMETER_NAME_LIMIT = "@limit";

    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres");

    /// <inheritdoc/>
    public async Task<AppendResult> AppendAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEvents();
            }

            using var connection = CreateConnection();
            using var command = CreateAppendEventCommand(connection, @event, ref appendCondition, options.Value.StorageSerializer);

            await connection.ConnectToEventStoreAsync(options.Value.Backoff, _logger, cancellationToken).ConfigureAwait(false);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.ConflictAppendingEvents();

                return AppendResult.Conflict;
            }

            var position = new EventPosition((ulong)reader.GetInt64(reader.GetOrdinal(ColumnNames.Position)));
            var timestamp = new DateTime(reader.GetDateTime(reader.GetOrdinal(ColumnNames.CreatedAt)).Ticks, DateTimeKind.Utc);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.EventsAppended();
            }

            return new(new DcbEventSequenced(position, timestamp, @event));
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEventsCancelled();
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.ErrorAppendEvents(ex);

            throw EventStoreException(ex);
        }
    }

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(IEnumerable<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events is not ICollection<DcbEvent> collection)
        {
            collection = [.. events];
        }

        return collection.Count switch
        {
            0 => Task.FromResult(AppendResult.Empty),
            1 => AppendAsync(collection.First(), appendCondition, cancellationToken),
            _ => AppendAsync(collection, appendCondition, cancellationToken),
        };
    }

    async Task<AppendResult> AppendAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEvents();
            }

            var count = events.Count;

            using var connection = CreateConnection();
            using var command = CreateAppendEventsCommand(connection, events, ref appendCondition, options.Value.StorageSerializer);

            if (command.Connection is { } conn && conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            var positions = new List<(EventPosition, DateTimeOffset)>(capacity: count);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var positionOrdinal = reader.GetOrdinal(ColumnNames.Position);
            var createAtOrdinal = reader.GetOrdinal(ColumnNames.CreatedAt);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var position = new EventPosition((ulong)reader.GetInt64(positionOrdinal));
                var timestamp = new DateTime(reader.GetDateTime(createAtOrdinal).Ticks, DateTimeKind.Utc);

                positions.Add((position, timestamp));
            }

            if (positions.Count == 0)
            {
                _logger.ConflictAppendingEvents();

                return AppendResult.Conflict;
            }

            if (positions.Count == count)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.EventsAppended();
                }

                var items = new DcbEventSequenced[count];

                var i = 0;

                foreach (var evt in events)
                {
                    var (position, timestamp) = positions[i];

                    items[i] = new(position, timestamp, evt);
                    i++;
                }

                return new(items);
            }

            throw UnexpectedRowsCountException(ref appendCondition, count, positions.Count);
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEventsCancelled();
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.ErrorAppendEvents(ex);

            throw EventStoreException(ex);
        }
    }

    static InvalidOperationException UnexpectedRowsCountException(ref AppendCondition appendCondition, int expectedCount, int count)
    {
        var ex = new InvalidOperationException("Unexpected rows count");

        ex.Data["Count"] = count;
        ex.Data["ExpectedCount"] = expectedCount;
        ex.Data["FailIfEventsMatch"] = appendCondition.FailIfEventsMatch;
        ex.Data["After"] = appendCondition.After;

        return ex;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition afterPosition) => new AsyncEnumerable(query, afterPosition, options, _logger);

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query) =>
        new StreamAsyncEnumerable(
            messageWriter: messageWriter,
            query: query,
            afterPosition: default,
            options: options,
            logger: _logger,
            storageEnumerable: null);

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition afterPosition) =>
        new StreamAsyncEnumerable(
            messageWriter: messageWriter,
            query: query,
            afterPosition: afterPosition,
            options: options,
            logger: _logger,
            storageEnumerable: ReadAsync(query, afterPosition));

    static NpgsqlCommand CreateReadEventsCommand(NpgsqlConnection connection, EventPosition afterPosition, DcbQuery query, int limit)
    {
        var command = connection.CreateCommand();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var sb = new ValueStringBuilder(initialCapacity: 512);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_read_events(");
        AppendAfterPosition(ref sb, afterPosition, command);
        sb.Append(", ");
        AppendQuery(ref sb, query, command);
        sb.Append($") LIMIT {COMMAND_PARAMETER_NAME_LIMIT};");

        command.AddParameter(COMMAND_PARAMETER_NAME_LIMIT, limit);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    static NpgsqlCommand CreateAppendEventCommand(
        NpgsqlConnection connection,
        DcbEvent @event,
        ref AppendCondition appendCondition,
        IStorageSerializer serializer)
    {
        var command = connection.CreateCommand();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var sb = new ValueStringBuilder(initialCapacity: 512);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_append_event(");
        AppendEvent(ref sb, @event, null, command, serializer); // -- dcb_event: hash -> name -> tag_hashes -> tags -> payload
        sb.Append(", ");
        AppendAfterPosition(ref sb, appendCondition.After, command); // -- p_cond_after
        sb.Append(", ");
        AppendQuery(ref sb, appendCondition.FailIfEventsMatch, command); // -- VARIADIC p_cond_fail_if_match
        sb.Append(");");
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    static NpgsqlCommand CreateAppendEventsCommand(
        NpgsqlConnection connection,
        IEnumerable<DcbEvent> events,
        ref AppendCondition appendCondition,
        IStorageSerializer serializer)
    {
        var command = connection.CreateCommand();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var sb = new ValueStringBuilder(initialCapacity: 512);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_append_events(array[");

        var idx = 0;

        foreach (var @event in events)
        {
            AppendEvent(ref sb, @event, (++idx).ToString("D", CultureInfo.InvariantCulture), command, serializer);
            sb.Append(',');
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_event[], ");
        AppendAfterPosition(ref sb, appendCondition.After, command);
        sb.Append(", ");
        AppendQuery(ref sb, appendCondition.FailIfEventsMatch, command);
        sb.Append(");");
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    static void AppendEvent(
        ref ValueStringBuilder sb,
        DcbEvent @event,
        string? eventIndex,
        NpgsqlCommand command,
        IStorageSerializer serializer)
    {
        string pName;

        sb.Append("(");

        pName = "@evh" + eventIndex;
        sb.Append(pName);
        sb.Append(',');
        command.AddParameter(pName, (int)@event.EventType.Hash);

        pName = "@evn" + eventIndex;
        sb.Append(pName);
        sb.Append(',');
        command.AddParameter(pName, @event.EventType.Value);

        var sbNames = new ValueStringBuilder(initialCapacity: 256);

        // tags
        sb.Append(" array[");

        if (@event.Tags is { IsEmpty: false } tags)
        {
            var idx = 0;

            foreach (var tag in tags)
            {
                var suffix = (++idx).ToString("D", CultureInfo.InvariantCulture);

                // hashes
                pName = "@tg" + eventIndex + 'h' + suffix;
                sb.Append(pName);
                sb.Append(',');
                command.AddParameter(pName, (long)tag.Hash);

                // names
                pName = "@tg" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, tag.Value);
            }

            sbNames.Length -= 1; // drop last symbol ','
            sb.Length -= 1; // drop last symbol ','
        }

        sb.Append("]::bigint[], array[");
        sb.Append(sbNames.AsSpan());
        sb.Append("]::text[], ");

        pName = "@pl" + eventIndex;
        sb.Append(pName);
        sb.Append("::json)::dcb_event");
        command.AddParameter(pName, Encoding.UTF8.GetString(serializer.Serialize(@event.Payload)));

        sbNames.Dispose();
    }

    static void AppendAfterPosition(ref ValueStringBuilder sb, EventPosition afterPosition, NpgsqlCommand command)
    {
        command.AddParameter(COMMAND_PARAMETER_NAME_AFTER, (long)afterPosition.Value);

        sb.Append(COMMAND_PARAMETER_NAME_AFTER);
    }

    static void AppendQuery(ref ValueStringBuilder sb, DcbQuery query, NpgsqlCommand command)
    {
        if (query.IsEmpty)
        {
            return;
        }

        sb.Append("VARIADIC array[");

        var sbNames = new ValueStringBuilder(initialCapacity: 256);

        var itemIdx = 0;

        foreach (var (eventTypes, tags) in query)
        {
            string pName;

            var eventIndex = (++itemIdx).ToString("D", CultureInfo.InvariantCulture);

            // even types
            sb.Append("(array[");

            var idx = 0;

            foreach (var eventType in eventTypes)
            {
                var suffix = (++idx).ToString("D", CultureInfo.InvariantCulture);

                // hashes
                pName = "@qev" + eventIndex + 'h' + suffix;

                sb.Append(pName);
                sb.Append(',');
                command.AddParameter(pName, (int)eventType.Hash);

                // names
                pName = "@qev" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, eventType.Value);
            }

            if (eventTypes.Count != 0)
            {
                sbNames.Length -= 1; // drop last symbol ','
                sb.Length -= 1; // drop last symbol ','
            }

            sb.Append("]");

            if (eventTypes.Count == 0)
            {
                sb.Append("::integer[]");
            }

            sb.Append(", array[");
            sb.Append(sbNames.AsSpan());
            sb.Append(']');

            if (eventTypes.Count == 0)
            {
                sb.Append("::text[]");
            }

            sbNames.Length = 0;

            // tags
            sb.Append(", array[");

            idx = 0;

            foreach (var tag in tags)
            {
                var suffix = (++idx).ToString("D", CultureInfo.InvariantCulture);

                // hashes
                pName = "@qtg" + eventIndex + 'h' + suffix;

                sb.Append(pName);
                sb.Append(',');
                command.AddParameter(pName, (long)tag.Hash);

                // names
                pName = "@qtg" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, tag.Value);
            }

            if (tags.Count != 0)
            {
                sbNames.Length -= 1; // drop last symbol ','
                sb.Length -= 1; // drop last symbol ','
            }

            sb.Append("]");

            if (tags.Count == 0)
            {
                sb.Append("::bigint[]");
            }

            sb.Append(", array[");
            sb.Append(sbNames.AsSpan());
            sb.Append("]");

            if (tags.Count == 0)
            {
                sb.Append("::text[]");
            }

            sb.Append("),");
            sbNames.Length = 0;
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_query_item[]");

        sbNames.Dispose();
    }

    static DcbEventSequenced InitialEnumeratorEvent(EventPosition afterPosition) => new(afterPosition, default, new DcbEvent
    {
        EventType = default,
        Payload = new object()
    });

    static DcbEventStoreException EventStoreException(Exception ex) => new(message: ex.Message, innerException: ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static NpgsqlConnection CreateConnection(EventStorePostgresOptions options) => new NpgsqlConnection(options.ConnectionString);

    NpgsqlConnection CreateConnection() => CreateConnection(options.Value);
}
