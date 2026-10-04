namespace Filin.Storage;

/// <summary>
/// Represents a read model projection that captures a materialized state computed from domain events.
/// </summary>
public sealed record Projection
{
    /// <summary>
    /// Gets the unique identifier of the projection.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="ProjectionId" path="/summary"/>
    /// </remarks>
    public ProjectionId Id { get; init; }

    /// <summary>
    /// Gets the timestamp of the last event applied to this projection.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the sequence or global stream position of the last event applied to this projection.
    /// </summary>
    public EventPosition Position { get; init; }

    /// <summary>
    /// Gets the state payload data of the projection.
    /// </summary>
    public required object Payload { get; init; }
}
