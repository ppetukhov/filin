using System.Diagnostics.CodeAnalysis;

using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Filin;

/// <summary>
/// When applied to a type, indicates that the type is a provider and that it should be automatically registered.
/// </summary>
/// <param name="name">The provider name, for example, <c>"PostgreStorage"</c>.</param>
/// <param name="kind">The kind of provider, for example, <c>"EventStore"</c>, <c>"ProjectionStore"</c>.</param>
/// <param name="type">Type of provider.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class RegisterProviderAttribute(
    string name,
    string kind,
    [DynamicallyAccessedMembers(PublicMethods | PublicConstructors)]
    Type type) : Attribute
{
    /// <summary>
    /// Gets the provider name, for example, <c>"PostgreStorage"</c>.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets the kind of the provider, for example, <c>"EventStore"</c>, <c>"ProjectionStore"</c>.
    /// </summary>
    public string Kind => kind;

    /// <summary>
    /// Gets the type used to configure the provider.
    /// </summary>
    public Type Type => type;
}
