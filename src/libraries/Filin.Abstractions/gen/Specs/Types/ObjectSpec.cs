using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs.Types;

sealed record ObjectSpec : TypeSpec
{
    internal ObjectSpec(INamedTypeSymbol type) : base(type)
    {
        DisplayParts = type.ToDisplayParts();
        Type = type;
    }

    internal INamedTypeSymbol Type { get; }

    internal ImmutableArray<SymbolDisplayPart> DisplayParts { get; }
}
