using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Represents a domain event container that encapsulates the event metadata, tags, and payload.
/// </summary>
public sealed record DcbEvent
{
    /// <inheritdoc cref="Filin.EventType" path="/summary"/>
    public required EventType EventType { get; init; }

    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the collection of tags associated with this event to form execution boundaries.
    /// </summary>
    public ImmutableSortedSet<Tag>? Tags { get; init; }

    /// <summary>
    /// Gets the payload containing the core data of the domain event.
    /// </summary>
    public required object Data { get; init; }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="DcbEvent"/> to a <see cref="DcbQueryItem"/>.
    /// </summary>
    /// <param name="evt">The domain event instance to convert.</param>
    /// <returns>A new <see cref="DcbQueryItem"/> built from the event metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="evt"/> is <see langword="null"/>.</exception>
    public static implicit operator DcbQueryItem(DcbEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return evt.ToDcbQueryItem();
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="DcbEvent"/> to a <see cref="DcbQuery"/>.
    /// </summary>
    /// <param name="evt">The domain event instance to convert.</param>
    /// <returns>A new <see cref="DcbQuery"/> defining the boundary based on this event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="evt"/> is <see langword="null"/>.</exception>
    public static implicit operator DcbQuery(DcbEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return evt.ToDcbQuery();
    }

    /// <summary>
    /// Converts this event instance into a single <see cref="DcbQueryItem"/> boundary definition.
    /// </summary>
    /// <returns>A <see cref="DcbQueryItem"/> containing this event's type and its associated tags.</returns>
    public DcbQueryItem ToDcbQueryItem() => new([EventType], Tags ?? []);

    /// <summary>
    /// Converts this event instance into a full <see cref="DcbQuery"/> dynamic consistency boundary specification.
    /// </summary>
    /// <returns>A composite <see cref="DcbQuery"/> initialized from this event's metadata.</returns>
    public DcbQuery ToDcbQuery() => ToDcbQueryItem().ToDcbQuery();
}
