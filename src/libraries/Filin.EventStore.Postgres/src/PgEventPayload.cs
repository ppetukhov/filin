using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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
    public int[]? TagHashes { get; init; }

    [JsonPropertyName(ColumnNames.TagNames)]
    public string[]? TagNames { get; init; }

    internal DcbEventSequenced ToEventSequenced()
    {
        var position = new EventPosition(Position);
        var timestamp = DateTime.SpecifyKind(TimestampUtc, DateTimeKind.Utc);
        var eventType = (EventType)Identity.UnsafeCreate(ToValueBytes(TypeName), TypeHash);
        ImmutableHashSet<Tag>? tags;

        if (TagNames is { Length: > 0 } tgNames && TagHashes is { Length: > 0 } tgHashes)
        {
            Debug.Assert(tgHashes.Length == tgHashes.Length);

            var tagsBulder = ImmutableHashSet.CreateBuilder<Tag>();

            for (var i = 0; i < tgNames.Length && i < tgHashes.Length; i++)
            {
                tagsBulder.Add((Tag)Identity.UnsafeCreate(ToValueBytes(tgNames[i]), tgHashes[i]));
            }

            tags = tagsBulder.ToImmutable();
        }
        else
        {
            tags = null;
        }

        return new DcbEventSequenced(new DcbEvent { EventType = eventType, Timestamp = timestamp, Tags = tags, Data = NullData }, position);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[]? ToValueBytes(string? id) => string.IsNullOrEmpty(id) ? null : Encoding.UTF8.GetBytes(id);
    }
}
