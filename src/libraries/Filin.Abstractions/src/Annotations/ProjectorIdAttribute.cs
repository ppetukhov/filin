namespace Filin;

/// <summary>
/// Specifies the unique identifier and schema version for an event projector.
/// </summary>
/// <param name="name">The unique name of the projector.</param>
/// <param name="version">The version of the projector projection logic.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ProjectorIdAttribute(string name, uint version) : Attribute
{
    /// <summary>
    /// Gets the unique name of the projector.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets or sets the version of the projector.
    /// </summary>
    /// <value>The default value is 0.</value>
    public uint Version { get; set; } = version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorIdAttribute"/> class with a default version of 0.
    /// </summary>
    /// <param name="name">The unique name of the projector.</param>
    public ProjectorIdAttribute(string name) : this(name, 0) { }
}
