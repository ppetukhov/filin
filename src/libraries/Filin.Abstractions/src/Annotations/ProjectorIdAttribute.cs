namespace Filin;

/// <summary>
/// Идентификатор проектора.
/// </summary>
/// <param name="name">Название.</param>
/// <param name="version">Версия.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ProjectorIdAttribute(string name, uint version) : Attribute
{
    /// <summary>
    /// Название.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Версия проектора.
    /// </summary>
    /// <remarks>Значение по умолчанию 0</remarks>
    public uint Version { get; set; } = version;

    /// <inheritdoc cref="ProjectorIdAttribute"/>
    public ProjectorIdAttribute(string name) : this(name, 0) { }
}
