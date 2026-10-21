using System.Collections.Immutable;

using Filin.Generators.Specs;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filin.Generators;

partial class CodeGenerator
{
    static class ProjectorIdParser
    {
        internal static IncrementalValuesProvider<ProjectorId> ParseEventTags(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(
                Constants.ProjectorIdAttribute,
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

                    var array = ImmutableArray.CreateBuilder<(string Name, uint Version, ObjectSpec TargetType)>();

                    foreach (var attr in ctx.Attributes)
                    {
                        ct.ThrowIfCancellationRequested();

                        var name = attr.ConstructorArguments[0].Value is string str ? str.Trim() : string.Empty;

                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        var version = 0u;

                        if (attr.ConstructorArguments.Length > 1)
                        {
                            version = (uint)attr.ConstructorArguments[1].Value!;
                        }

                        array.Add((name, version, targetType));
                    }

                    return array.ToImmutable();
                })
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; })
                .Select(static (item, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    var (name, version, targetType) = item;

                    var idFormated = name;

                    if (version is not 0)
                    {
                        idFormated = $"{name}{Constants.VersionSeparator}{version}";
                    }

                    return new ProjectorId(
                        TargetType: targetType,
                        Id: idFormated,
                        Raw: (ProjectorName: name, Version: version));
                }).WithTrackingName($"select_{nameof(ProjectorId)}")
                .Collect()
                .SelectMany(static (eventTypes, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    return eventTypes
                        .Sort(ProjectorIdComparer.Instance)
                        .Distinct(ProjectorIdComparer.Instance).ToImmutableArray();
                }).WithTrackingName($"select_unique_fields_for_{nameof(ProjectorId)}");

        sealed class ProjectorIdComparer : IEqualityComparer<ProjectorId>, IComparer<ProjectorId>
        {
            internal static readonly ProjectorIdComparer Instance = new();

            public int Compare(ProjectorId x, ProjectorId y) => ObjectSpecComparer.Instance.Compare(x.TargetType, y.TargetType);
            public bool Equals(ProjectorId x, ProjectorId y) => ObjectSpecComparer.Instance.Equals(x.TargetType, y.TargetType);
            public int GetHashCode(ProjectorId obj) => ObjectSpecComparer.Instance.GetHashCode(obj.TargetType);
        }
    }
}
