using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

sealed record ObjectSpec(INamedTypeSymbol Type) : TypeSpec(Type)
{
    internal ImmutableArray<SymbolDisplayPart> DisplayParts { get; } = Type.ToDisplayParts();
}
