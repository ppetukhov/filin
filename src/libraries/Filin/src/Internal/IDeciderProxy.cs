namespace Filin.Internal;

/// <inheritdoc cref="IDecider{TCommand, TState}"/>
interface IDeciderProxy<TCommand, TState>
    where TCommand : notnull, ICommand<TState>
    where TState : notnull
{
    /// <inheritdoc cref="IDecider{TCommand, TState}.Decide(TCommand)"/>
    DecideResult Decide(TCommand command);

    /// <inheritdoc cref="IDecider{TCommand, TState}.Decide(TCommand, TState)"/>
    DecideResult Decide(TCommand command, TState state);
}
