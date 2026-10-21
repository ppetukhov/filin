namespace Filin;

/// <summary>
/// Defines a domain decider responsible for evaluating business rules and processing commands 
/// against a specific state of type <typeparamref name="TState"/>.
/// </summary>
/// <typeparam name="TCommand">The type of the command to execute.</typeparam>
/// <typeparam name="TState">The type of the aggregate or domain state.</typeparam>
public interface IDecider<TCommand, TState>
    where TCommand : notnull, ICommand<TState>
    where TState : notnull
{
    /// <summary>
    /// Evaluates business rules to initiate a brand-new state from the ground up based on the provided <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The command containing intent and data to initialize the state.</param>
    /// <returns>A result containing either the structured event submission entry or a domain business error.</returns>
    static abstract DecideResult Decide(TCommand command);

    /// <summary>
    /// Evaluates business rules against the current <paramref name="state"/> and the incoming <paramref name="command"/> to transition the state.
    /// </summary>
    /// <param name="command">The command containing intent and data to modify the current state.</param>
    /// <param name="state">The existing domain state required to evaluate the business logic decisions.</param>
    /// <returns>A result containing either the structured event submission entry or a domain business error.</returns>
    static abstract DecideResult Decide(TCommand command, TState state);
}
