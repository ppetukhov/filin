namespace Filin;

/// <summary>
/// Represents a domain event that has been successfully committed to the <see cref="Storage.IEventStore"/>, 
/// paired with its absolute storage position context.
/// </summary>
/// <param name="Event"><inheritdoc cref="DcbEvent" path="/summary"/></param>
/// <param name="Position"><inheritdoc cref="EventPosition" path="/summary"/></param>
public readonly record struct DcbEventSequenced(DcbEvent Event, EventPosition Position);
