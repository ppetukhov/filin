namespace Filin.Storage;

/// <summary>
/// Проекция.
/// </summary>
public sealed record Projection
{
    /// <summary>
    /// <inheritdoc cref="ProjectionId" path="/summary"/>
    /// </summary>
    public ProjectionId Id { get; init; }

    /// <summary>
    /// Время последнего применённого к проекции события.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Номер последнего применённого к проекции события.
    /// </summary>
    public EventPosition Position { get; init; }

    /// <summary>
    /// Данные проекции.
    /// </summary>
    public required object Payload { get; init; }
}
