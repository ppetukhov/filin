namespace Filin;

/// <summary>
/// Метаданные проекции.
/// </summary>
public sealed record ProjectionMetadata
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
}
