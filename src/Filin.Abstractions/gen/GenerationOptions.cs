using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Filin.Generators;

sealed record GenerationOptions
{
    /// <summary>
    /// Gets the root namespace of the current project.
    /// </summary>
    public string RootNamespace { get; init; } = "Filin";

    /// <summary>
    /// EventType extensions classname.
    /// </summary>
    public string EventTypeExtensionsClass { get; init; } = "EventTypeExtensions";

    /// <summary>
    /// EventTypeToDcbEvent extensions classname.
    /// </summary>
    public string EventTypeToDcbEventExtensionsClass { get; init; } = "EventTypeToDcbEventExtensions";

    /// <summary>
    /// Queries extensions classname.
    /// </summary>
    public string QueryExtensionsClass { get; init; } = "QueryExtensions";

    /// <summary>
    /// Tag extensions classname.
    /// </summary>
    public string TagExtensionsClass { get; init; } = "TagExtensions";

    internal static GenerationOptions GetGenerationOptions(AnalyzerConfigOptionsProvider options, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var opt = new GenerationOptions();

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.RootNamespace, out var emittingNamespace))
        {
            opt = opt with { RootNamespace = emittingNamespace! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.EventTypeExtensionsClass, out var eventTypeExtensionsClass))
        {
            opt = opt with { EventTypeExtensionsClass = eventTypeExtensionsClass! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.EventTypeToDcbEventExtensionsClass, out var eventTypeToDcbEventExtensionsClass))
        {
            opt = opt with { EventTypeToDcbEventExtensionsClass = eventTypeToDcbEventExtensionsClass! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.QueryExtensionsClass, out var queryExtensionsClass))
        {
            opt = opt with { QueryExtensionsClass = queryExtensionsClass! };
        }

        if (options.TryGetStringValueMSBuildProperty(MSBuildProperties.TagExtensionsClass, out var tagExtensionsClass))
        {
            opt = opt with { TagExtensionsClass = tagExtensionsClass! };
        }

        return opt;
    }
}
