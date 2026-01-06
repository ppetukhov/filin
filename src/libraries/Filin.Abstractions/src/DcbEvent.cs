using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Событие доменной области.
/// </summary>
public sealed record DcbEvent
{
    /// <inheritdoc cref="Filin.EventType" path="/summary"/>
    public required EventType EventType { get; init; }

    /// <summary>
    /// Дата и время события.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Тэги события.
    /// </summary>
    public ImmutableHashSet<Tag>? Tags { get; init; }

    /// <summary>
    /// Полезные данные события.
    /// </summary>
    public required object Data { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        string? tags = Tags is not null ? '[' + string.Join(", ", Tags.Select(x => x.ToString())) + ']' : null;

        return $"{nameof(DcbEvent)} {{ {nameof(EventType)} = {EventType}, {nameof(Timestamp)} = {Timestamp.LocalDateTime}, {nameof(Tags)} = {tags}, {nameof(Data)} = {Data} }}";
    }

    public static implicit operator DcbQueryItem(DcbEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return evt.ToDcbQueryItem();
    }

    public static implicit operator DcbQuery(DcbEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return evt.ToDcbQuery();
    }

    public DcbQueryItem ToDcbQueryItem() => new([EventType], Tags ?? []);

    public DcbQuery ToDcbQuery() => ToDcbQueryItem().ToDcbQuery();
}
