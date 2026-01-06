using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Элемент фильтра <see cref="DcbQuery"/>.
/// </summary>
/// <remarks>Тип события может быть любым из коллекции <paramref name="EventTypes"/>, если она не пуста.
/// В противном случае тип события не учитывается при фильтрации.
/// И событие должно содержать все тэги из коллекции <paramref name="Tags"/>, если она не пуста.
/// В противном случае тэги события не учитываются при фильтрации.
/// </remarks>
/// <param name="EventTypes">Типы событий <see cref="EventType"/> применяются с оператором <see langword="or"/>.</param>
/// <param name="Tags">Тэги событий <see cref="Tag"/> применяются с оператором <see langword="and"/>.</param>
public sealed record DcbQueryItem(ImmutableHashSet<EventType> EventTypes, ImmutableHashSet<Tag> Tags)
{
    public static readonly DcbQueryItem Any = new([], []);

    public bool IsAny => this == Any;

    public DcbQueryItem(EventType eventType) : this([eventType], []) { }

    public DcbQueryItem(IEnumerable<EventType> eventTypes) : this([.. eventTypes], []) { }

    public DcbQueryItem(Tag tag) : this([], [tag]) { }

    public DcbQueryItem(IEnumerable<Tag> tags) : this([], [.. tags]) { }

    public DcbQueryItem(IEnumerable<EventType> eventTypes, IEnumerable<Tag> tags) : this([.. eventTypes], [.. tags]) { }

    public DcbQueryItem Or(EventType eventType) => this with { EventTypes = EventTypes.Add(eventType) };

    public DcbQueryItem Or(IEnumerable<EventType> eventTypes) => this with { EventTypes = EventTypes.Union(eventTypes) };

    public DcbQueryItem And(Tag tag) => this with { Tags = Tags.Add(tag) };

    public DcbQueryItem And(IEnumerable<Tag> tags) => this with { Tags = Tags.Union(tags) };

    public DcbQueryItem And(EventType eventType, Tag tag) => this with { EventTypes = EventTypes.Add(eventType), Tags = Tags.Add(tag) };

    public DcbQueryItem And(EventType eventType, IEnumerable<Tag> tags) => this with { EventTypes = EventTypes.Add(eventType), Tags = Tags.Union(tags) };

    public DcbQueryItem And(IEnumerable<EventType> eventTypes, Tag tag) => this with { EventTypes = EventTypes.Union(eventTypes), Tags = Tags.Add(tag) };

    public DcbQueryItem And(IEnumerable<EventType> eventTypes, IEnumerable<Tag> tags) => this with { EventTypes = EventTypes.Union(eventTypes), Tags = Tags.Union(tags) };

    public bool MatchesEvent(DcbEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return (EventTypes.IsEmpty || EventTypes.Contains(@event.EventType)) && (Tags.IsEmpty || @event.Tags is { IsEmpty: false } tags && Tags.All(tags.Contains));
    }

    public static implicit operator DcbQuery(DcbQueryItem queryItem)
    {
        ArgumentNullException.ThrowIfNull(queryItem);

        return queryItem.ToDcbQuery();
    }

    public DcbQuery ToDcbQuery() => new([this]);
}
