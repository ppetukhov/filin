using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace SourceGenerators;

static class TypeModelHelper
{
    internal static List<ITypeSymbol>? GetAllTypeArgumentsInScope(this INamedTypeSymbol type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        List<ITypeSymbol>? args = null;
        TraverseContainingTypes(type);
        return args;

        void TraverseContainingTypes(INamedTypeSymbol current)
        {
            if (current.ContainingType is INamedTypeSymbol parent)
            {
                TraverseContainingTypes(parent);
            }

            if (!current.TypeArguments.IsEmpty)
            {
                (args ??= new()).AddRange(current.TypeArguments);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetFullyQualifiedName(this ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
