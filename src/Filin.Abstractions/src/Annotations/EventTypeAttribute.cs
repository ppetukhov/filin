namespace Filin;

/// <summary>
/// Specifies the unique domain identifier and version for an event.
/// </summary>
/// <remarks>
/// The combination of event type and version must be unique within the domain boundaries.
/// </remarks>
/// <param name="eventType">The string identifier representing the type of the event.</param>
/// <param name="version">The schema version of the event type.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class EventTypeAttribute(string eventType, uint version) : Attribute
{
    /// <summary>
    /// Gets the unique event type identifier.
    /// </summary>
    public string EventType => eventType;

    /// <summary>
    /// Gets the schema version number of the event type.
    /// </summary>
    public uint Version => version;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeAttribute"/> class with a default version of 0.
    /// </summary>
    /// <param name="eventType">The string identifier representing the type of the event.</param>
    public EventTypeAttribute(string eventType) : this(eventType, 0) { }
}
