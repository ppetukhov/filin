using Filin.Internal;

namespace Filin.Configuration;

sealed class FilinOptions
{
    /// <summary>
    /// key is <c>TState</c>, value is type of instance <see cref="IProjectorProxy{TState}"/>.
    /// </summary>
    public Dictionary<Type, Type> ProjectorProxyTypes { get; } = [];

    /// <summary>
    /// Get dictionary key: cortage command type with state type and value is type of instance <see cref="IDeciderProxy{TCommand, TState}"/>.
    /// </summary>
    public Dictionary<(Type Command, Type State), Type> DeciderProxyTypes { get; } = [];
}
