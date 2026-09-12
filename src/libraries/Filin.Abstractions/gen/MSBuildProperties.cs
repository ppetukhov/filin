namespace Filin.Generators;

/// <summary>
/// Exposes the available Filin MSBuild properties.
/// </summary>
static class MSBuildProperties
{
    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.EmittingNamespace"/>
    /// </summary>
    public const string EmittingNamespace = $"FilinEmittingNamespace";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.EventTypeExtensionsClassName"/>
    /// </summary>
    public const string EventTypeExtensionsClassName = $"FilinEventTypeExtensionsClassName";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.EventTypeToDcbEventExtensionsClassName"/>
    /// </summary>
    public const string EventTypeToDcbEventExtensionsClassName = $"FilinEventTypeToDcbEventExtensionsClassName";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.GetEventTypeMethod" path="/summary"/>
    /// </summary>
    public const string GetEventTypeMethod = $"FilinGetEventTypeMethod";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.GetQueryMethod"/>
    /// </summary>
    public const string GetQueryMethod = $"FilinGetQueryMethod";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.GetProjectionIdMethod"/>
    /// </summary>
    public const string GetProjectionIdMethod = $"FilinGetProjectionIdMethod";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.GetTagMethod"/>
    /// </summary>
    public const string GetTagMethod = $"FilinGetTagMethod";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.GetTagsMethod"/>
    /// </summary>
    public const string GetTagsMethod = $"FilinGetTagsMethod";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.ProjectorIdProperty"/>
    /// </summary>
    public const string ProjectorIdProperty = $"FilinProjectorIdProperty";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.QueryExtensionsClass"/>
    /// </summary>
    public const string QueryExtensionsClass = $"FilinQueryExtensions";

    /// <summary>
    /// The MSBuild property for <see cref="GenerationOptions.TagExtensionsClassName"/>
    /// </summary>
    public const string TagExtensionsClassName = $"FilinTagExtensionsClassName";
}
