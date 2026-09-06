namespace Filin.Internal;

/// <inheritdoc cref="IDeciderInvoker{TCommand, TState}"/>
sealed class DeciderInvoker<TDecider, TCommand, TState> : IDeciderInvoker<TCommand, TState>
    where TDecider : IDecider<TCommand, TState>
    where TCommand : notnull, ICommand<TState>
    where TState : notnull
{
    /// <inheritdoc/>
    public DecideResult Decide(TCommand command) => TDecider.Decide(command);

    /// <inheritdoc/>
    public DecideResult Decide(TCommand command, TState state) => TDecider.Decide(command, state);
}
