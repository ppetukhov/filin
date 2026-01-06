using Microsoft.CodeAnalysis;

using SourceGenerators;

namespace Filin.Generators.Specs.Members;

sealed record PropertySpec(IPropertySymbol property) : MemberSpec(property, new TypeRef(property.Type))
{
    public bool IsStatic { get; } = property.IsStatic;

    public override bool CanGet { get; } = property.GetMethod is { DeclaredAccessibility: >= Accessibility.Internal };
}
