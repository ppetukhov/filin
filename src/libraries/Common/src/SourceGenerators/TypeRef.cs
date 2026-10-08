using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace SourceGenerators;

/// <summary>
/// An equatable value representing type identity.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
sealed class TypeRef(ITypeSymbol type) : IEquatable<TypeRef>
{
    public string Name => type.Name;

    /// <summary>
    /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
    /// </summary>
    public string FullyQualifiedName => type.GetFullyQualifiedName();

    public bool IsValueType => type.IsValueType;

    public SpecialType SpecialType => type.OriginalDefinition.SpecialType;

    public bool Equals(TypeRef? other) => other != null && FullyQualifiedName == other.FullyQualifiedName;

    public override bool Equals(object? obj) => Equals(obj as TypeRef);

    public override int GetHashCode() => FullyQualifiedName.GetHashCode();
}
