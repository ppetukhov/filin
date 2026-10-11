using System.Diagnostics;

using Filin.Generators.Extensions;

using Microsoft.CodeAnalysis;

namespace Filin.Generators.Specs;

/// <summary>
/// An equatable value representing type identity.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
readonly record struct TypeRef(ITypeSymbol Type) : IEquatable<TypeRef>
{
    public string Name => Type.Name;

    /// <summary>
    /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
    /// </summary>
    public string FullyQualifiedName => Type.GetFullyQualifiedName();

    public bool IsValueType => Type.IsValueType;

    public SpecialType SpecialType => Type.OriginalDefinition.SpecialType;

    public bool Equals(TypeRef? other) => other != null && FullyQualifiedName == other.Value.FullyQualifiedName;

    //public override bool Equals(object? obj) => Equals(obj as TypeRef);

    public override int GetHashCode() => FullyQualifiedName.GetHashCode();
}
