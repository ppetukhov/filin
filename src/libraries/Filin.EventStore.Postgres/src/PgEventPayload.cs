using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Filin.EventStore.Postgres;

sealed record PgEventPayload
{
    static readonly object NullData = new object();

    [JsonPropertyName(ColumnNames.Position)]
    public ulong Position { get; init; }

    [JsonPropertyName(ColumnNames.TimestampUtc)]
    public DateTime TimestampUtc { get; init; }

    [JsonPropertyName(ColumnNames.TypeHash)]
    public int TypeHash { get; init; }

    [JsonPropertyName(ColumnNames.TypeName)]
    public required string TypeName { get; init; }

    [JsonPropertyName(ColumnNames.TagHashes)]
    public long[]? TagHashes { get; init; }

    [JsonPropertyName(ColumnNames.TagNames)]
    public string[]? TagNames { get; init; }

    internal DcbEventSequenced ToEventSequenced()
    {
        var position = new EventPosition(Position);
        var timestamp = DateTime.SpecifyKind(TimestampUtc, DateTimeKind.Utc);
#pragma warning disable CS0618 // Type or member is obsolete
        var eventType = EventType.UnsafeCreate(TypeName, (uint)TypeHash);
#pragma warning restore CS0618 // Type or member is obsolete
        ImmutableSortedSet<Tag>? tags;

        if (TagNames is { Length: > 0 } tgNames && TagHashes is { Length: > 0 } tgHashes)
        {
            Debug.Assert(tgHashes.Length == tgHashes.Length);

            var tagsBulder = ImmutableSortedSet.CreateBuilder<Tag>();

            for (var i = 0; i < tgNames.Length && i < tgHashes.Length; i++)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                tagsBulder.Add((Tag)IdSpan.UnsafeCreate(tgNames[i], (ulong)tgHashes[i]));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            tags = tagsBulder.ToImmutable();
        }
        else
        {
            tags = null;
        }

        return new DcbEventSequenced(position, new DcbEvent { EventType = eventType, Timestamp = timestamp, Tags = tags, Data = NullData });
    }
}
