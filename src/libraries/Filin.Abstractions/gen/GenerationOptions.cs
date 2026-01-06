using Microsoft.CodeAnalysis.Diagnostics;

using Filin.Generators.Extensions;

namespace Filin.Generators;

sealed record GenerationOptions
{
    /// <summary>
    /// Emitting namespace. <inheritdoc cref="Constants.DefaultEmittingNamespace" path="/summary"/>
    /// </summary>
    public string EmittingNamespace { get; init; } = Constants.DefaultEmittingNamespace;

    /// <summary>
    /// EventType extensions classname. <inheritdoc cref="Constants.DefaultEventTypeExtensionsClassName" path="/summary"/>
    /// </summary>
    public string EventTypeExtensionsClassName { get; init; } = Constants.DefaultEventTypeExtensionsClassName;

    /// <summary>
    /// Getting Tag method. <inheritdoc cref="Constants.DefaultGetEventTypeMethod" path="/summary"/>
    /// </summary>
    public string GetEventTypeMethod { get; init; } = Constants.DefaultGetEventTypeMethod;

    /// <summary>
    /// Getting Tag method. <inheritdoc cref="Constants.DefaultQueryMethod" path="/summary"/>
    /// </summary>
    public string GetQueryMethod { get; init; } = Constants.DefaultQueryMethod;

    /// <summary>
    /// Getting Tag method. <inheritdoc cref="Constants.DefaultGetProjectionIdMethod" path="/summary"/>
    /// </summary>
    public string GetProjectionIdMethod { get; init; } = Constants.DefaultGetProjectionIdMethod;

    /// <summary>
    /// Getting Tag method. <inheritdoc cref="Constants.DefaultGetTagMethod" path="/summary"/>
    /// </summary>
    public string GetTagMethod { get; init; } = Constants.DefaultGetTagMethod;

    /// <summary>
    /// Getting Tags method. <inheritdoc cref="Constants.DefaultGetTagsMethod" path="/summary"/>
    /// </summary>
    public string GetTagsMethod { get; init; } = Constants.DefaultGetTagsMethod;

    /// <summary>
    /// Getting ProjectorId property. <inheritdoc cref="Constants.DefaultProjectorIdProperty" path="/summary"/>
    /// </summary>
    public string ProjectorIdProperty { get; init; } = Constants.DefaultProjectorIdProperty;

    /// <summary>
    /// Queries extensions classname. <inheritdoc cref="Constants.DefaultQueryExtensionsClass" path="/summary"/>
    /// </summary>
    public string QueryExtensionsClass { get; init; } = Constants.DefaultQueryExtensionsClass;

    /// <summary>
    /// Tag extensions classname. <inheritdoc cref="Constants.DefaultTagExtensionsClassName" path="/summary"/>
    /// </summary>
    public string TagExtensionsClassName { get; init; } = Constants.DefaultTagExtensionsClassName;

    internal static GenerationOptions GetGenerationOptions(AnalyzerConfigOptionsProvider options, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var opt = new GenerationOptions();

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.EmittingNamespace, out var emittingNamespace))
        {
            opt = opt with { EmittingNamespace = emittingNamespace! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.GetEventTypeMethod, out var getEventTypeMethod))
        {
            opt = opt with { GetEventTypeMethod = getEventTypeMethod! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.EventTypeExtensionsClassName, out var eventTypeExtensionsClassName))
        {
            opt = opt with { EventTypeExtensionsClassName = eventTypeExtensionsClassName! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.GetQueryMethod, out var getQueryMethod))
        {
            opt = opt with { GetQueryMethod = getQueryMethod! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.GetProjectionIdMethod, out var getProjectionIdMethod))
        {
            opt = opt with { GetProjectionIdMethod = getProjectionIdMethod! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.GetTagMethod, out var getTagMethod))
        {
            opt = opt with { GetTagMethod = getTagMethod! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.GetTagsMethod, out var getTagsMethod))
        {
            opt = opt with { GetTagMethod = getTagsMethod! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.ProjectorIdProperty, out var projectorIdProperty))
        {
            opt = opt with { ProjectorIdProperty = projectorIdProperty! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.QueryExtensionsClass, out var queryExtensionsClass))
        {
            opt = opt with { QueryExtensionsClass = queryExtensionsClass! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.TagExtensionsClassName, out var tagExtensionsClassName))
        {
            opt = opt with { TagExtensionsClassName = tagExtensionsClassName! };
        }

        return opt;
    }
}
