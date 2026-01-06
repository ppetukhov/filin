using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

using Filin.Generators.Extensions;

using SourceGenerators;

namespace Filin.Generators.Specs.Types;

[DebuggerDisplay("Name={DisplayString}, Kind={SpecKind}")]
abstract record TypeSpec(ITypeSymbol type)
{
    public Accessibility Accessibility { get; } = type.GetGlobalAccessibility();

    public TypeRef TypeRef { get; } = new TypeRef(type);

    /// <summary>
    /// <see cref="Type.FullName"/> like rendering of the type name.
    /// </summary>
    public string FullName { get; } = type.GetFullyQualifiedName();

    public string IdentifierCompatibleSubstring { get; } = type.ToIdentifierCompatibleSubstring();

    public SymbolFlag Flags { get; } = GetFlag(type);

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
