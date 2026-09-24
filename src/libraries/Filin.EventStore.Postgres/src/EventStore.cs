using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

using Filin.AdoNet;
using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres;

/// <summary>
/// Реализация хранилища <see cref="IEventStore"/> в PostgreSQL.
/// </summary>
/// <param name="dataSource"></param>
/// <param name="messageWriter"><inheritdoc cref="ChannelWriter{T}" path="/summary"/></param>
/// <param name="loggerFactory"><inheritdoc cref="ILoggerFactory" path="/summary"/></param>
/// <param name="options"><inheritdoc cref="IOptions{TOptions}" path="/summary"/></param>
sealed partial class EventStore(
    DbDataSource dataSource,
    ChannelWriter<Message> messageWriter,
    ILoggerFactory loggerFactory,
    IOptions<EventStorePostgresOptions> options) : IEventStore, IAsyncDisposable
{
    const string COMMAND_PARAMETER_NAME_AFTER = "@aft";
    const string COMMAND_PARAMETER_NAME_LIMIT = "@limit";

    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres");

    readonly AppendEventsProcessor _appenEventsProcessor = new(options.Value.AppendBoundedCapacity, loggerFactory.CreateLogger("Filin.EventStore.Postgres"));

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var command = CreateAppendEventCommand(dataSource, @event, ref appendCondition, options.Value.StorageSerializer);

        return _appenEventsProcessor.AppendEvent(command, @event, cancellationToken);
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

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition afterPosition) => new AsyncEnumerable(dataSource, query, afterPosition, options, _logger);

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

    public ValueTask DisposeAsync() => _appenEventsProcessor.DisposeAsync();

    static DbCommand CreateReadEventsCommand(DbDataSource dataSource, EventPosition afterPosition, DcbQuery query, int limit)
    {
        var command = dataSource.CreateCommand();

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

    static DbCommand CreateAppendEventCommand(
        DbDataSource dataSource,
        DcbEvent @event,
        ref AppendCondition appendCondition,
        IStorageSerializer serializer)
    {
        var command = dataSource.CreateCommand();

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

    static DbCommand CreateAppendEventsCommand(
        DbDataSource dataSource,
        IEnumerable<DcbEvent> events,
        ref AppendCondition appendCondition,
        IStorageSerializer serializer)
    {
        var command = dataSource.CreateCommand();

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
        DbCommand command,
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

    static void AppendAfterPosition(ref ValueStringBuilder sb, EventPosition afterPosition, DbCommand command)
    {
        command.AddParameter(COMMAND_PARAMETER_NAME_AFTER, (long)afterPosition.Value);

        sb.Append(COMMAND_PARAMETER_NAME_AFTER);
    }

    static void AppendQuery(ref ValueStringBuilder sb, DcbQuery query, DbCommand command)
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

    Task<AppendResult> AppendAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        var command = CreateAppendEventsCommand(dataSource, events, ref appendCondition, options.Value.StorageSerializer);

        return _appenEventsProcessor.AppendEvents(command, events, cancellationToken);
    }
}
