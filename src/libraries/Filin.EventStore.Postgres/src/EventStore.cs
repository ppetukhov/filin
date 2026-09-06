using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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

            var lockTimeoutRetries = 0;

            while (lockTimeoutRetries < options.Value.MaxAppendRetries)
            {
                await connection.ConnectToEventStoreAsync(options.Value.Backoff, _logger, cancellationToken).ConfigureAwait(false);

                try
                {
                    using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        _logger.ConflictAppendingEvents();

                        return AppendResult.Conflict;
                    }

                    var pos = new EventPosition((ulong)reader.GetInt64(reader.GetOrdinal(ColumnNames.Position)));

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.EventsAppended();
                    }

                    return new(new DcbEventSequenced(pos, @event));
                }
                catch (DbException dbex) when (dbex.SqlState is "DCB01") // lock timeout in the event store.
                {
                    lockTimeoutRetries++;
                    continue;
                }
            }

            throw new InvalidOperationException("Max retries was reached");
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEventsCancelled();
            }

            throw;
        }
        catch (Exception ex) when (ex is not DcbConflictException)
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

            var lockTimeoutRetries = 0;

            while (lockTimeoutRetries < options.Value.MaxAppendRetries)
            {
                var positions = new List<EventPosition>(capacity: count);

                try
                {
                    using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    var positionOrdinal = reader.GetOrdinal(ColumnNames.Position);

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        positions.Add(new EventPosition((ulong)reader.GetInt64(positionOrdinal)));
                    }
                }
                catch (DbException dbex) when (dbex.SqlState is "DCB01") // Time out lock event store.
                {
                    positions.Clear();
                    lockTimeoutRetries++;
                    continue;
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
                        var pos = positions[i];

                        items[i] = new(pos, evt);
                        i++;
                    }

                    return new(items);
                }
                else if (positions.Count == 0)
                {
                    _logger.ConflictAppendingEvents();

                    return AppendResult.Conflict;
                }

                ThrowUnexpectedRowsCount(ref appendCondition, count, positions.Count);
            }

            throw new InvalidOperationException("Max retries was reached");
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AppendingEventsCancelled();
            }

            throw;
        }
        catch (Exception ex) when (ex is not DcbConflictException)
        {
            _logger.ErrorAppendEvents(ex);

            throw EventStoreException(ex);
        }
    }

    [DoesNotReturn]
    static void ThrowUnexpectedRowsCount(ref AppendCondition appendCondition, int expectedCount, int count)
    {
        var ex = new InvalidOperationException("Unexpected rows count");

        ex.Data["Count"] = count;
        ex.Data["ExpectedCount"] = expectedCount;
        ex.Data["FailIfEventsMatch"] = appendCondition.FailIfEventsMatch;
        ex.Data["After"] = appendCondition.After;

        throw ex;
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
        sb.Append("SELECT * from read_events(");
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
        sb.Append("SELECT * from append_event(");
        AppendEvent(ref sb, @event, null, command, serializer);
        sb.Append(", ");
        AppendAfterPosition(ref sb, appendCondition.After, command);
        sb.Append(", ");
        AppendQuery(ref sb, appendCondition.FailIfEventsMatch, command);
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
        sb.Append("SELECT * from append_events(array[");

        var idx = 0;

        foreach (var @event in events)
        {
            AppendEvent(ref sb, @event, (++idx).ToString("D", CultureInfo.InvariantCulture), command, serializer);
            sb.Append(',');
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_event_raw[], ");
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

        sb.Append('(');

        pName = "@evt" + eventIndex;
        sb.Append(pName);
        sb.Append(',');
        command.AddParameter(pName, @event.Timestamp.ToUniversalTime());

        pName = "@evh" + eventIndex;
        sb.Append(pName);
        sb.Append(',');
        command.AddParameter(pName, @event.EventType.GetHashCode());

        pName = "@evn" + eventIndex;
        sb.Append(pName);
        sb.Append(',');
        command.AddParameter(pName, @event.EventType.ToString() ?? string.Empty);

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
                command.AddParameter(pName, tag.GetHashCode());

                // names
                pName = "@tg" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, tag.ToString() ?? string.Empty);
            }

            sbNames.Length -= 1; // drop last symbol ','
            sb.Length -= 1; // drop last symbol ','
        }

        sb.Append("], array[");
        sb.Append(sbNames.AsSpan());
        sb.Append("], ");

        pName = "@pl" + eventIndex;
        sb.Append(pName);
        sb.Append(")::dcb_event_raw");
        command.AddParameter(pName, serializer.Serialize(@event.Data).ToArray());

        sbNames.Dispose();
    }

    static void AppendAfterPosition(ref ValueStringBuilder sb, EventPosition afterPosition, NpgsqlCommand command)
    {
        command.AddParameter(COMMAND_PARAMETER_NAME_AFTER, afterPosition.Value);

        sb.Append(COMMAND_PARAMETER_NAME_AFTER);
    }

    static void AppendQuery(ref ValueStringBuilder sb, DcbQuery query, NpgsqlCommand command)
    {
        if (query.Items.IsEmpty)
        {
            return;
        }

        sb.Append("VARIADIC array[");

        var sbNames = new ValueStringBuilder(initialCapacity: 256);

        var itemIdx = 0;

        foreach (var (eventTypes, tags) in query.Items)
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
                command.AddParameter(pName, eventType.GetHashCode());

                // names
                pName = "@qev" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, eventType.ToString() ?? string.Empty);
            }

            if (eventTypes.Count != 0)
            {
                sbNames.Length -= 1; // drop last symbol ','
                sb.Length -= 1; // drop last symbol ','
            }

            sb.Append("]");

            if (eventTypes.Count == 0)
            {
                sb.Append("::dcb_event_type_hash[]");
            }

            sb.Append(", array[");
            sb.Append(sbNames.AsSpan());
            sb.Append(']');

            if (eventTypes.Count == 0)
            {
                sb.Append("::dcb_event_type_name[]");
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
                command.AddParameter(pName, tag.GetHashCode());

                // names
                pName = "@qtg" + eventIndex + 'n' + suffix;
                sbNames.Append(pName);
                sbNames.Append(',');
                command.AddParameter(pName, tag.ToString() ?? string.Empty);
            }

            if (tags.Count != 0)
            {
                sbNames.Length -= 1; // drop last symbol ','
                sb.Length -= 1; // drop last symbol ','
            }

            sb.Append("]");

            if (tags.Count == 0)
            {
                sb.Append("::dcb_event_tag_hash[]");
            }

            sb.Append(", array[");
            sb.Append(sbNames.AsSpan());
            sb.Append("]");

            if (tags.Count == 0)
            {
                sb.Append("::dcb_event_tag_name[]");
            }

            sb.Append("),");
            sbNames.Length = 0;
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_query_item[]");

        sbNames.Dispose();
    }

    static DcbEventSequenced InitialEnumeratorEvent(EventPosition afterPosition) => new(afterPosition, new DcbEvent
    {
        EventType = default,
        Timestamp = default,
        Data = new object()
    });

    static DcbEventStoreException EventStoreException(Exception ex) => new(message: ex.Message, innerException: ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static NpgsqlConnection CreateConnection(EventStorePostgresOptions options) => new NpgsqlConnection(options.ConnectionString);

    NpgsqlConnection CreateConnection() => CreateConnection(options.Value);
}
