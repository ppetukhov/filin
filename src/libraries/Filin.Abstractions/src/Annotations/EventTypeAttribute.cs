namespace Filin;

/// <summary>
/// Тип события.
/// </summary>
/// <remarks>Уникальный в пределах домена идентификатор типа события.</remarks>
/// <param name="eventType">Тип события.</param>
/// <param name="version">Версия события.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class EventTypeAttribute(string eventType, uint version) : Attribute
{
    /// <summary>
    /// Тип события.
    /// </summary>
    public string EventType { get; } = eventType;

    /// <summary>
    /// Номер версии типа события.
    /// </summary>
    public uint Version { get; } = version;

    /// <param name="eventType">Тип события.</param>
    public EventTypeAttribute(string eventType) : this(eventType, 0) { }
}
