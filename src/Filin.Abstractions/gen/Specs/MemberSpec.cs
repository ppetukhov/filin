using System.Diagnostics;

using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

abstract record MemberSpec
{
    public MemberSpec(ISymbol member, TypeRef typeRef)
    {
        Debug.Assert(member is IPropertySymbol or IParameterSymbol);
        Accessibility = member.GetGlobalAccessibility();
        Name = member.Name;
        TypeRef = typeRef;
    }

    public Accessibility Accessibility { get; }

    public string Name { get; }

    public TypeRef TypeRef { get; }

    public abstract bool CanGet { get; }
}
