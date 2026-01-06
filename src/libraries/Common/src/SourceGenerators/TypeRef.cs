using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace SourceGenerators;

/// <summary>
/// An equatable value representing type identity.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
sealed class TypeRef(ITypeSymbol type) : IEquatable<TypeRef>
{
    public string Name { get; } = type.Name;

    /// <summary>
    /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
    /// </summary>
    public string FullyQualifiedName { get; } = type.GetFullyQualifiedName();

    public bool IsValueType { get; } = type.IsValueType;

    public TypeKind TypeKind { get; } = type.TypeKind;

    public SpecialType SpecialType { get; } = type.OriginalDefinition.SpecialType;

    public bool CanBeNull => !IsValueType || SpecialType is SpecialType.System_Nullable_T;

    public bool Equals(TypeRef? other) => other != null && FullyQualifiedName == other.FullyQualifiedName;

    public override bool Equals(object? obj) => Equals(obj as TypeRef);

    public override int GetHashCode() => FullyQualifiedName.GetHashCode();
}
