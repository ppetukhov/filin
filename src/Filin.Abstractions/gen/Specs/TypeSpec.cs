using System.Diagnostics;
using System.Runtime.CompilerServices;

using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

[DebuggerDisplay("Name={DisplayString}, Kind={SpecKind}")]
abstract record TypeSpec(ITypeSymbol TypeSymbol)
{
    public Accessibility Accessibility { get; } = TypeSymbol.GetGlobalAccessibility();

    public TypeRef TypeRef { get; } = new TypeRef(TypeSymbol);

    public string FullName { get; } = TypeSymbol.GetFullyQualifiedName();

    public string IdentifierCompatibleSubstring { get; } = TypeSymbol.ToIdentifierCompatibleSubstring();

    public SymbolFlag Flags { get; } = GetFlag(TypeSymbol);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static SymbolFlag GetFlag(ISymbol symbol)
    {
        var flag = default(SymbolFlag);

        if (symbol.IsStatic)
        {
            flag |= SymbolFlag.IsStatic;
        }

        if (symbol.IsAbstract)
        {
            flag |= SymbolFlag.IsAbstract;
        }

        if (symbol.IsImplicitlyDeclared)
        {
            flag |= SymbolFlag.IsImplicitlyDeclared;
        }

        return flag;
    }
}
