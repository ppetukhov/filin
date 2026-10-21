namespace Filin;

/// <summary>
/// Represents a domain event that has been successfully committed to the <see cref="Storage.IEventStore"/>, 
/// paired with its absolute storage position context.
/// </summary>
/// <param name="Position"><inheritdoc cref="EventPosition" path="/summary"/></param>
/// <param name="Timestamp">The date and time when the domain event occurred.</param>
/// <param name="Event"><inheritdoc cref="DcbEvent" path="/summary"/></param>
public readonly record struct DcbEventSequenced(EventPosition Position, DateTimeOffset Timestamp, DcbEvent Event);
