namespace Filin;

/// <summary>
/// Defines a domain command representing an intent to mutate the state of a <typeparamref name="TState"/> model.
/// </summary>
/// <remarks>
/// Commands are processed against a specific state instance according to business rules, 
/// resulting in either new domain events or a failed state transition.
/// </remarks>
/// <typeparam name="TState">The type of the domain state model target for this command.</typeparam>
public interface ICommand<TState> : IWithQueryTags where TState : notnull;
