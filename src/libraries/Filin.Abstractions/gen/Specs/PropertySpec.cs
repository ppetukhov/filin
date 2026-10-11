using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

sealed record PropertySpec(IPropertySymbol Property) : MemberSpec(Property, new TypeRef(Property.Type))
{
    public override bool CanGet { get; } = Property.GetMethod is { DeclaredAccessibility: >= Accessibility.Internal };
}
