namespace Filin;

/// <summary>
/// Represents a domain event container that encapsulates the event metadata, tags, and payload.
/// </summary>
public sealed record DcbEvent
{
    /// <inheritdoc cref="Filin.EventType" path="/summary"/>
    public required EventType EventType { get; init; }

    /// <summary>
    /// Gets the collection of tags associated with this event to form execution boundaries.
    /// </summary>
    public Tags Tags { get; init; }

    /// <summary>
    /// Gets the payload containing the core data of the domain event.
    /// </summary>
    public required object Payload { get; init; }
}
