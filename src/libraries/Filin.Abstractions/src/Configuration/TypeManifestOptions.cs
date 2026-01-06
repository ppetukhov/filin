namespace Filin.Configuration;

/// <summary>
/// Configuration of all types which are known to the code generator.
/// </summary>
public sealed class TypeManifestOptions
{
    /// <summary>
    /// Gets event types.
    /// </summary>
    public Dictionary<EventType, Type> EventTypes { get; } = [];

    /// <summary>
    /// Get dictionary key: state type and value: projector type.
    /// </summary>
    public Dictionary<Type, Type> ProjectorTypes { get; } = [];

    /// <summary>
    /// Gets projection types.
    /// </summary>
    public Dictionary<ProjectorId, Type> ProjectionTypes { get; } = [];

    /// <summary>
    /// Get dictionary key: cortage command type with state type and value: decider type.
    /// </summary>
    public Dictionary<(Type Command, Type State), Type> DeciderTypes { get; } = [];

    /// <summary>
    /// Gets type manifest provider identifiers, which are used.
    /// </summary>
    public HashSet<object> TypeManifestProviders { get; } = [];
}
