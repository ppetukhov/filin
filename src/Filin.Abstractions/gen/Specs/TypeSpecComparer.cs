namespace Filin.Generators.Specs;

sealed class TypeSpecComparer : IEqualityComparer<TypeSpec>
{
    internal static readonly TypeSpecComparer Instance = new();

    public bool Equals(TypeSpec x, TypeSpec y) => string.Equals(x.IdentifierCompatibleSubstring, y.IdentifierCompatibleSubstring, StringComparison.Ordinal);

    public int GetHashCode(TypeSpec obj) => obj?.IdentifierCompatibleSubstring.GetHashCode() ?? 0;
}
