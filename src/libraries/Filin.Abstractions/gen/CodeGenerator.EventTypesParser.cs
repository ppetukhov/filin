using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Filin.Generators.Specs.Types;

namespace Filin.Generators;

sealed partial class CodeGenerator
{
    sealed class EventTypesParser
    {
        internal static IncrementalValuesProvider<EventType> ParseEventTypes(SyntaxValueProvider valueProvider) =>
            valueProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: Constants.EventTypeAttribute,
                predicate: static (node, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

#pragma warning disable IDE0072 // Add missing cases
                    return node.Kind() switch
                    {
                        SyntaxKind.RecordDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration => true,
                        _ => false
                    };
#pragma warning restore IDE0072 // Add missing cases
                },
                transform: static (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    if (ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol)
                    {
                        return [];
                    }

                    var targetType = new ObjectSpec(namedTypeSymbol);

                    var array = ImmutableArray.CreateBuilder<(string EventTypeName, uint Version, ObjectSpec TargetType)>();

                    foreach (var attr in ctx.Attributes)
                    {
                        ct.ThrowIfCancellationRequested();

                        var eventTypeName = attr.ConstructorArguments[0].Value is string str ? str.Trim() : string.Empty;

                        if (string.IsNullOrEmpty(eventTypeName))
                        {
                            continue;
                        }

                        var version = 0u;

                        if (attr.ConstructorArguments.Length > 1)
                        {
                            version = (uint)attr.ConstructorArguments[1].Value!;
                        }

                        array.Add((eventTypeName, version, targetType));
                    }

                    return array.ToImmutable();
                })
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; })
                .WithTrackingName($"transform_EventTypeAttribute_to_EventTypeRaw")
                .Select(static (evt, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    var (eventTypeName, version, targetType) = evt;

                    var fieldName = Emitter.SanitizeIdentifierName(evt.EventTypeName);

                    var eventTypeNameFormated = eventTypeName;

                    if (version is not 0)
                    {
                        fieldName = $"{fieldName}_V{version}";

                        eventTypeNameFormated = $"{eventTypeName}{Constants.EventTypeVersionSeparator}{version}";
                    }

                    return new EventType(
                        FieldName: fieldName,
                        TargetType: targetType,
                        EventTypeName: eventTypeNameFormated,
                        Raw: (EventTypeName: eventTypeName, Version: version));
                }).WithTrackingName($"select_{nameof(EventType)}")
                .Collect()
                .SelectMany(static (eventTypes, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    return eventTypes
                        .OrderBy(static x => x.EventTypeName, StringComparer.Ordinal)
                        .Distinct(EventTypeFieldNameComparer.Instance).ToImmutableArray();
                }).WithTrackingName($"select_unique_fields_for_{nameof(EventType)}");

        sealed class EventTypeFieldNameComparer : IEqualityComparer<EventType>
        {
            internal static readonly EventTypeFieldNameComparer Instance = new EventTypeFieldNameComparer();
            public bool Equals(EventType x, EventType y) => string.Equals(x.FieldName, y.FieldName, StringComparison.Ordinal);
            public int GetHashCode(EventType obj) => obj.FieldName.GetHashCode();
        }
    }
}
