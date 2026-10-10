using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

using Filin.AdoNet;
using Filin.EventStore.AdoNet.Internal;
using Filin.Storage;

namespace Filin.EventStore.AdoNet.Postgres;

sealed class PgDbCommandFactory(DbDataSource dataSource, IStorageSerializer serializer) : DbCommandFactory
{
    static readonly ConcurrentDictionary<CacheKey, string> ReadCommandTextCache = new();
    static readonly ConcurrentDictionary<CacheKey, string> AppendCommandTextCache = new();
    static readonly string[] PlaceHolders = [.. Enumerable.Range(1, 64).Select(static pos => "$" + pos)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string GetPlaceHolder(int position) => position < PlaceHolders.Length ? PlaceHolders[position] : "$" + (position + 1);

    static string CreateReadCommandText(CacheKey querykey)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
#pragma warning restore CA2000 // Dispose objects before losing scope

        var position = 0;
        sb.Append("SELECT * from dcb_read_events(");
        AppendPlaceHolder(ref sb, ref position); // AppendAfterPosition
        AppendQuery(ref sb, ref position, ref querykey);
        sb.Append($") LIMIT ");
        sb.Append(GetPlaceHolder(position++)); // LIMIT
        sb.Append($";");

        return sb.ToString();
    }

    internal override DbCommand CreateAppendEventCommand(DcbEvent @event, AppendCondition appendCondition)
    {
        if (CacheKey.TryCreate(@event, appendCondition.FailIfEventsMatch, out var querykey))
        {
            var commandText = AppendCommandTextCache.GetOrAdd(querykey, CreateAppendCommandText);
            var command = dataSource.CreateCommand(commandText);

            AppendAfterPosition(appendCondition.After, command);
            AppendEvent(@event, command, serializer);
            AppendQuery(appendCondition.FailIfEventsMatch, command);

            return command;
        }

        return CreateAppendEventCommandSlow(@event, ref appendCondition);
    }

    internal override DbCommand CreateAppendEventsCommand(ICollection<DcbEvent> events, AppendCondition appendCondition)
    {
        if (events.Count == 1)
        {
            return CreateAppendEventCommand(events.First(), appendCondition);
        }

        if (CacheKey.TryCreate(events, appendCondition.FailIfEventsMatch, out var querykey))
        {
            var commandText = AppendCommandTextCache.GetOrAdd(querykey, CreateAppendCommandText);
            var command = dataSource.CreateCommand(commandText);

            AppendAfterPosition(appendCondition.After, command);

            foreach (var @event in events)
            {
                AppendEvent(@event, command, serializer);
            }

            AppendQuery(appendCondition.FailIfEventsMatch, command);

            return command;
        }

        return CreateAppendEventsCommandSlow(events, ref appendCondition);
    }

    internal override DbCommand CreateReadEventsCommand(EventPosition afterPosition, DcbQuery query, int limit)
    {
        if (CacheKey.TryCreate(query, out var querykey))
        {
            var commandText = ReadCommandTextCache.GetOrAdd(querykey, CreateReadCommandText);
            var command = dataSource.CreateCommand(commandText);

            AppendAfterPosition(afterPosition, command);
            AppendQuery(query, command);
            command.AddParameter(limit);

            return command;
        }

        return CreateReadEventsCommandSlow(afterPosition, query, limit);
    }

    DbCommand CreateReadEventsCommandSlow(EventPosition afterPosition, DcbQuery query, int limit)
    {
        var command = dataSource.CreateCommand();
#pragma warning disable CA2000 // Dispose objects before losing scope
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_read_events(");

        var position = 0;
        AppendAfterPosition(ref sb, ref position, afterPosition, command);
        AppendQuery(ref sb, ref position, query, command);
        sb.Append($") LIMIT ");
        sb.Append(GetPlaceHolder(position++)); // LIMIT
        sb.Append($";");

        command.AddParameter(limit);
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    DbCommand CreateAppendEventCommandSlow(DcbEvent @event, ref AppendCondition appendCondition)
    {
        var command = dataSource.CreateCommand();
#pragma warning disable CA2000 // Dispose objects before losing scope
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_append_event(");

        var position = 0;
        AppendAfterPosition(ref sb, ref position, appendCondition.After, command); // -- p_cond_after
        AppendEvent(ref sb, @event, ref position, command, serializer); // -- dcb_event: hash -> name -> tag_hashes -> tagsCount -> payload
        sb.Append(", ");
        AppendQuery(ref sb, ref position, appendCondition.FailIfEventsMatch, command); // -- VARIADIC p_cond_fail_if_match
        sb.Append(");");
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    DbCommand CreateAppendEventsCommandSlow(IEnumerable<DcbEvent> events, ref AppendCondition appendCondition)
    {
        var command = dataSource.CreateCommand();

#pragma warning disable CA2000 // Dispose objects before losing scope
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
#pragma warning restore CA2000 // Dispose objects before losing scope
        sb.Append("SELECT * from dcb_append_events(array[");

        var position = 0;
        AppendAfterPosition(ref sb, ref position, appendCondition.After, command);

        foreach (var @event in events)
        {
            AppendEvent(ref sb, @event, ref position, command, serializer);
            sb.Append(',');
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_event[], ");
        AppendQuery(ref sb, ref position, appendCondition.FailIfEventsMatch, command);
        sb.Append(");");
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = sb.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return command;
    }

    static string CreateAppendCommandText(CacheKey querykey)
    {
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);

        var eventTagsCountSpan = CacheKey.EventTagsCount(ref querykey);

        var position = 0;

        if (eventTagsCountSpan.Length == 1)
        {
            sb.Append("SELECT * from dcb_append_event(");
            AppendPlaceHolder(ref sb, ref position); // AppendAfterPosition
            AppendEvent(ref sb, ref position, eventTagsCountSpan[0]);
            sb.Append(", ");
        }
        else
        {
            sb.Append("SELECT * from dcb_append_events(");
            AppendPlaceHolder(ref sb, ref position); // AppendAfterPosition
            sb.Append(" array[");

            foreach (var eventTagsCount in eventTagsCountSpan)
            {
                AppendEvent(ref sb, ref position, eventTagsCount);
                sb.Append(',');
            }

            sb.Length -= 1; // drop last symbol ','

            sb.Append("]::dcb_event[], ");
        }

        AppendQuery(ref sb, ref position, ref querykey);
        sb.Append(");");

        return sb.ToString();
    }

    static void AppendEvent(ref ValueStringBuilder sb, ref int position, byte eventTagsCount)
    {
        sb.Append("(");
        AppendPlaceHolder(ref sb, ref position);
        AppendPlaceHolder(ref sb, ref position);

        Span<char> buf = stackalloc char[64];

        var sbNames = new ValueStringBuilder(buf);

        // tagsCount
        sb.Append(" array[");

        if (eventTagsCount != 0)
        {
            // hashes
            for (var idx = 1; idx <= eventTagsCount; idx++)
            {
                AppendPlaceHolder(ref sb, ref position);
            }
            sb.Length -= 1; // drop last symbol ','

            // names
            for (var idx = 1; idx <= eventTagsCount; idx++)
            {
                AppendPlaceHolder(ref sbNames, ref position);
            }
            sbNames.Length -= 1; // drop last symbol ','
        }

        sb.Append("], array[");
        sb.Append(sbNames.AsSpan());
        sb.Append("], ");

        sb.Append(GetPlaceHolder(position++));
        sb.Append("::json)::dcb_event");

        sbNames.Dispose();
    }

    static void AppendEvent(
        DcbEvent @event,
        DbCommand command,
        IStorageSerializer serializer)
    {
        var commandParams = command.Parameters;

        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (int)@event.EventType.Hash));
        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, @event.EventType.Value));

        // tags
        if (@event.Tags is { IsEmpty: false } tags)
        {
            // hashes
            foreach (var tag in tags)
            {
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (long)tag.Hash));
            }

            // names
            foreach (var tag in tags)
            {
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, tag.Value));
            }
        }

        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, Encoding.UTF8.GetString(serializer.Serialize(@event.Payload))));
    }

    static void AppendEvent(
        ref ValueStringBuilder sb,
        DcbEvent @event,
        ref int position,
        DbCommand command,
        IStorageSerializer serializer)
    {
        sb.Append("(");
        AppendPlaceHolder(ref sb, ref position);
        AppendPlaceHolder(ref sb, ref position);

        var commandParams = command.Parameters;

        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (int)@event.EventType.Hash));
        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, @event.EventType.Value));

        Span<char> buf = stackalloc char[64];

        var sbNames = new ValueStringBuilder(buf);

        // tags
        sb.Append(" array[");

        if (@event.Tags is { IsEmpty: false } tags)
        {
            foreach (var tag in tags)
            {
                // hashes
                AppendPlaceHolder(ref sb, ref position);
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (long)tag.Hash));
            }
            sb.Length -= 1; // drop last symbol ','

            foreach (var tag in tags)
            {
                // names
                AppendPlaceHolder(ref sbNames, ref position);
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, tag.Value));
            }
            sbNames.Length -= 1; // drop last symbol ','
        }

        var span = sbNames.AsSpan();
        sb.Append("], array[");
        sb.Append(span);
        sb.Append("], ");

        sb.Append(GetPlaceHolder(position++));
        sb.Append("::json)::dcb_event");
        _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, Encoding.UTF8.GetString(serializer.Serialize(@event.Payload))));

        sbNames.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendPlaceHolder(ref ValueStringBuilder sb, ref int position)
    {
        sb.Append(GetPlaceHolder(position++));
        sb.Append(',');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendAfterPosition(EventPosition afterPosition, DbCommand command) => command.AddParameter((long)afterPosition.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendAfterPosition(ref ValueStringBuilder sb, ref int position, EventPosition afterPosition, DbCommand command)
    {
        AppendPlaceHolder(ref sb, ref position);
        AppendAfterPosition(afterPosition, command);
    }

    static void AppendQuery(ref ValueStringBuilder sb, ref int position, ref CacheKey querykey)
    {
        var failIfMatch = CacheKey.FailIfMatch(ref querykey);

        if (failIfMatch.IsEmpty)
        {
            return;
        }

        sb.Append("VARIADIC array[");

        Span<char> buf = stackalloc char[64];
        var sbNames = new ValueStringBuilder(buf);

        foreach (var (eventTypesCount, tagsCount) in failIfMatch)
        {
            // eventTypes
            if (eventTypesCount == 0)
            {
                sb.Append("(array[]::integer[], array[]::text[]");
            }
            else
            {
                sb.Append("(array[");

                // hashes
                for (var idx = 1; idx <= eventTypesCount; idx++)
                {
                    AppendPlaceHolder(ref sb, ref position);
                }
                sb.Length -= 1; // drop last symbol ','

                // names
                for (var idx = 1; idx <= eventTypesCount; idx++)
                {
                    AppendPlaceHolder(ref sbNames, ref position);
                }
                sbNames.Length -= 1; // drop last symbol ','

                sb.Append("], array[");
                sb.Append(sbNames.AsSpan());
                sb.Append(']');

                sbNames.Length = 0;
            }

            // tags
            if (tagsCount == 0)
            {
                sb.Append(", array[]::bigint[], array[]::text[]),");
            }
            else
            {
                sb.Append(", array[");

                // hashes
                for (var idx = 1; idx <= tagsCount; idx++)
                {
                    AppendPlaceHolder(ref sb, ref position);
                }
                sb.Length -= 1; // drop last symbol ','

                // names
                for (var idx = 1; idx <= tagsCount; idx++)
                {
                    AppendPlaceHolder(ref sbNames, ref position);
                }
                sbNames.Length -= 1; // drop last symbol ','

                sb.Append("], array[");
                sb.Append(sbNames.AsSpan());
                sb.Append("]),");

                sbNames.Length = 0;
            }
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_query_item[]");

        sbNames.Dispose();
    }

    static void AppendQuery(DcbQuery query, DbCommand command)
    {
        if (query.IsEmpty)
        {
            return;
        }

        var commandParams = command.Parameters;

        foreach (var (eventTypes, tags) in query)
        {
            // event types
            foreach (var eventType in eventTypes)
            {
                // hashes
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (int)eventType.Hash));
            }

            foreach (var eventType in eventTypes)
            {
                // names
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, eventType.Value));
            }

            // tags
            foreach (var tag in tags)
            {
                // hashes
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (long)tag.Hash));
            }

            foreach (var tag in tags)
            {
                // names
                _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, tag.Value));
            }
        }
    }

    static void AppendQuery(ref ValueStringBuilder sb, ref int position, DcbQuery query, DbCommand command)
    {
        if (query.IsEmpty)
        {
            return;
        }

        sb.Append("VARIADIC array[");

        Span<char> buf = stackalloc char[64];
        var sbNames = new ValueStringBuilder(buf);

        var commandParams = command.Parameters;

        foreach (var (eventTypes, tags) in query)
        {
            // even types
            if (eventTypes.IsEmpty)
            {
                sb.Append("(array[]::integer[], array[]::text[]");
            }
            else
            {
                sb.Append("(array[");

                // hashes
                foreach (var eventType in eventTypes)
                {
                    AppendPlaceHolder(ref sb, ref position);
                    _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (int)eventType.Hash));
                }
                sb.Length -= 1; // drop last symbol ','

                // names
                foreach (var eventType in eventTypes)
                {
                    AppendPlaceHolder(ref sbNames, ref position);
                    _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, eventType.Value));
                }
                sbNames.Length -= 1; // drop last symbol ','

                sb.Append("], array[");
                sb.Append(sbNames.AsSpan());
                sb.Append(']');

                sbNames.Length = 0;
            }

            // tags
            if (tags.IsEmpty)
            {
                sb.Append(", array[]::bigint[], array[]::text[]),");
            }
            else
            {
                sb.Append(", array[");

                // hashes
                foreach (var tag in tags)
                {
                    AppendPlaceHolder(ref sb, ref position);
                    _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, (long)tag.Hash));
                }
                sb.Length -= 1; // drop last symbol ','

                // names
                foreach (var tag in tags)
                {
                    AppendPlaceHolder(ref sbNames, ref position);
                    _ = commandParams.Add(command.CreateParameter(ParameterDirection.Input, tag.Value));
                }
                sbNames.Length -= 1; // drop last symbol ','

                sb.Append("], array[");
                sb.Append(sbNames.AsSpan());
                sb.Append("]),");

                sbNames.Length = 0;
            }
        }

        sb.Length -= 1; // drop last symbol ','

        sb.Append("]::dcb_query_item[]");

        sbNames.Dispose();
    }
}
