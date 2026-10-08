namespace Filin.Generators.Specs.Types;

sealed class ObjectSpecComparer : IEqualityComparer<ObjectSpec>, IComparer<ObjectSpec>
{
    internal static readonly ObjectSpecComparer Instance = new();

    public int Compare(ObjectSpec x, ObjectSpec y) => string.CompareOrdinal(x.IdentifierCompatibleSubstring, y.IdentifierCompatibleSubstring);

    public bool Equals(ObjectSpec x, ObjectSpec y) => string.Equals(x.IdentifierCompatibleSubstring, y.IdentifierCompatibleSubstring, StringComparison.Ordinal);

    public int GetHashCode(ObjectSpec obj) => obj?.IdentifierCompatibleSubstring.GetHashCode() ?? 0;
}
