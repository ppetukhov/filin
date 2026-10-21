namespace Filin.Generators;

/// <summary>
/// Exposes the available Filin MSBuild properties.
/// </summary>
static class MSBuildProperties
{
    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.RootNamespace"/>
    /// </summary>
    public const string RootNamespace = "RootNamespace";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.EventTypeExtensionsClass"/>
    /// </summary>
    public const string EventTypeExtensionsClass = "FilinEventTypeExtensionsClass";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.EventTypeToDcbEventExtensionsClass"/>
    /// </summary>
    public const string EventTypeToDcbEventExtensionsClass = "FilinEventTypeToDcbEventExtensionsClass";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.QueryExtensionsClass"/>
    /// </summary>
    public const string QueryExtensionsClass = "QueryExtensionsClass";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.TagExtensionsClass"/>
    /// </summary>
    public const string TagExtensionsClass = "FilinTagExtensionsClass";
}
