using System.Collections.Immutable;

using Filin.Generators.Specs.Types;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class DeciderParser
    {
        internal static IncrementalValuesProvider<Decider> ParseDeciders(SyntaxValueProvider syntaxValueProvider)
            => syntaxValueProvider.ForAttributeWithMetadataName(
                Constants.DeciderAttribute,
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

                    return ImmutableArray.Create(new Decider(targetType));
                })
                .SelectMany(static (array, ct) => { ct.ThrowIfCancellationRequested(); return array; })
                .Collect()
                .SelectMany(static (eventTypes, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    return eventTypes
                        .Sort(DeciderComparer.Instance)
                        .Distinct(DeciderComparer.Instance)
                        .ToImmutableArray();
                }).WithTrackingName($"select_unique_{nameof(Decider)}");

        sealed class DeciderComparer : IEqualityComparer<Decider>, IComparer<Decider>
        {
            internal static readonly DeciderComparer Instance = new();

            public int Compare(Decider x, Decider y) => ObjectSpecComparer.Instance.Compare(x.TargetType, y.TargetType);
            public bool Equals(Decider x, Decider y) => ObjectSpecComparer.Instance.Equals(x.TargetType, y.TargetType);
            public int GetHashCode(Decider obj) => ObjectSpecComparer.Instance.GetHashCode(obj.TargetType);
        }
    }
}
