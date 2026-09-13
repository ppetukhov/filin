using System.Buffers;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Filin.EventStore.Postgres;

sealed record PgEventPayload
{
    static readonly object NullData = new object();

    [JsonPropertyName(ColumnNames.Position)]
    public ulong Position { get; init; }

    [JsonPropertyName(ColumnNames.CreatedAt)]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName(ColumnNames.EventTypeHash)]
    public int EventTypeHash { get; init; }

    [JsonPropertyName(ColumnNames.EventType)]
    public required string EventType { get; init; }

    [JsonPropertyName(ColumnNames.TagHashes)]
    public long[]? TagHashes { get; init; }

    [JsonPropertyName(ColumnNames.Tags)]
    public string[]? Tags { get; init; }

    internal DcbEventSequenced ToEventSequenced()
    {
        var position = new EventPosition(Position);
        var timestamp = DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc);
#pragma warning disable CS0618 // Type or member is obsolete
        var eventType = Filin.EventType.UnsafeCreate(EventType, (uint)EventTypeHash);
#pragma warning restore CS0618 // Type or member is obsolete
        TagCollection tags;

        if (Tags is { Length: > 0 } tgNames && TagHashes is { Length: > 0 } tgHashes)
        {
            Debug.Assert(tgHashes.Length == tgHashes.Length);

            Tag[]? rented = null;

            try
            {
                rented = ArrayPool<Tag>.Shared.Rent(TagHashes.Length);

                for (var i = 0; i < tgNames.Length && i < tgHashes.Length; i++)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    rented[i] = (Tag)IdSpan.UnsafeCreate(tgNames[i], (ulong)tgHashes[i]);
#pragma warning restore CS0618 // Type or member is obsolete
                }

                tags = [.. rented];
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<Tag>.Shared.Return(rented);
                }
            }
        }
        else
        {
            tags = default;
        }

        return new DcbEventSequenced(position, timestamp, new DcbEvent { EventType = eventType, Tags = tags, Payload = NullData });
    }
}
