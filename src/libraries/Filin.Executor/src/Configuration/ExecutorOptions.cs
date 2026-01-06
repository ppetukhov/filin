namespace Filin.Configuration;

sealed class ExecutorOptions
{
    /// <summary>
    /// Key is <c>TState</c>, value is type of instance <see cref="IProjectorInvoker{TState}"/>.
    /// </summary>
    public Dictionary<Type, Type> ProjectorInvokerTypes { get; } = [];

    /// <summary>
    /// Get dictionary key: cortage command type with state type and value is type of instance <see cref="IDeciderInvoker{TCommand, TState}"/>.
    /// </summary>
    public Dictionary<(Type Command, Type State), Type> DeciderInvokerTypes { get; } = [];
}
