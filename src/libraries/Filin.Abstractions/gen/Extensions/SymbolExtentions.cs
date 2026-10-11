using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Extensions;

static class SymbolExtentions
{
    static readonly SymbolDisplayFormat IdentifierCompatibleFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    extension(ISymbol symbol)
    {
        internal Accessibility GetGlobalAccessibility()
        {
            var accessibility = Accessibility.Public;

            do
            {
                var access = symbol.DeclaredAccessibility;

                if (access != Accessibility.NotApplicable && access < accessibility)
                {
                    accessibility = access;
                }

                symbol = symbol.ContainingType;
            } while (symbol is not null && symbol.Kind is not SymbolKind.Namespace);

            return accessibility;
        }
    }
    extension(INamedTypeSymbol type)
    {
        internal List<ITypeSymbol>? GetAllTypeArgumentsInScope()
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
    }

    extension(ITypeSymbol type)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal string GetFullyQualifiedName() => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <summary>
        /// Returns unique identifier compatible name based on the fully qualified type name, including fully qualified type arguments.
        /// Names are truncated at 500 characters. (1023 is maximum identifier length)
        /// </summary>
        /// <remarks>
        /// When names are longer than 500 characters a hash code of the original string is appended to maintain identifier uniqueness.
        /// Examples:
        /// Namespace1.Namespace2.Type -> Namespace1__Namespace2__Type ;
        /// Namespace.GenericType&lt;Namespace.Type&gt; -> Namespace__GenericType___Namespace__Type___ ;
        /// </remarks>
        internal string ToIdentifierCompatibleSubstring()
        {
            var sb = new StringBuilder(50);

            ToIdentifierCompatibleSubstringBuilder(type, sb);

            //Replace . with __; remove invalid characters
            _ = sb.Replace(".", "__").Replace("[", "").Replace("]", "");

            // string.GetHashCode uses randomized string hashing.
            // Causes false negatives in Baseline tests if threshold is too small
            if (sb.Length > 500)
            {
                var hash = ((uint)sb.ToString().GetHashCode()).ToString("D10", System.Globalization.NumberFormatInfo.InvariantInfo);
                _ = sb.Remove(490, sb.Length - 490).Append(hash);
            }

            return sb.ToString();
        }
    }

    static void ToIdentifierCompatibleSubstringBuilder(ITypeSymbol type, StringBuilder displaySubstring)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            var rank = arrayType.Rank;
            var suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
            ToIdentifierCompatibleSubstringBuilder(arrayType.ElementType, displaySubstring);
            _ = displaySubstring.Append(suffix);
        }

        _ = displaySubstring.Append(type.ToDisplayString(IdentifierCompatibleFormat));

        if (type is not INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return;
        }

        if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
        {
            _ = displaySubstring.Append("___");

            foreach (var genericArg in typeArgsInScope)
            {
                ToIdentifierCompatibleSubstringBuilder(genericArg, displaySubstring);
            }

            _ = displaySubstring.Append("___");
        }

        return;
    }
}
