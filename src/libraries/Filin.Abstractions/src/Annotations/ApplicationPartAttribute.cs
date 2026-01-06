namespace Filin;

/// <summary>
/// Specifies an assembly to be added as an application part.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ApplicationPartAttribute" />.
/// </remarks>
/// <param name="assemblyName">The assembly name.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ApplicationPartAttribute(string assemblyName) : Attribute
{
    /// <summary>
    /// Gets the assembly name.
    /// </summary>
    public string AssemblyName { get; } = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
}
