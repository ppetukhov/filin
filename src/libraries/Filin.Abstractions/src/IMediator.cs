namespace Filin;

/// <summary>
/// Defines a centralized mediator responsible for coordinating and dispatching domain commands and queries.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Asynchronously dispatches a command to modify state after evaluating domain business rules.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command containing execution intent and payload metadata.</typeparam>
    /// <typeparam name="TState">The underlying type of the domain aggregate or state being evaluated.</typeparam>
    /// <param name="command">The concrete command instance to be processed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during execution processing.</param>
    /// <exception cref="DcbEventStoreException"/>
    /// <exception cref="DcbProjectorException"/>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <exception cref="MediatorException"/>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> wrapping a <see cref="ExecuteResult{TState}"/>, yielding the brand-new 
    /// state <typeparamref name="TState"/> if successful, or a structured domain business error on failure.
    /// </returns>
    Task<ExecuteResult<TState>> ExecuteAsync<TCommand, TState>(TCommand command, CancellationToken cancellationToken)
        where TState : notnull
        where TCommand : notnull, ICommand<TState>;

    /// <summary>
    /// Asynchronously processes a query request to retrieve or build a specific read-model projection.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query request specifying retrieval constraints.</typeparam>
    /// <typeparam name="TProjection">The targeted type of the read-model projection data.</typeparam>
    /// <param name="query">The concrete query request mapping the projection filter criteria.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during projection resolution.</param>
    /// <exception cref="DcbEventStoreException"/>
    /// <exception cref="DcbProjectorException"/>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <exception cref="MediatorException"/>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing the resolved read-model <typeparamref name="TProjection"/>, 
    /// or <see langword="null"/> if no matching state projection exists.
    /// </returns>
    Task<TProjection?> QueryAsync<TQuery, TProjection>(TQuery query, CancellationToken cancellationToken)
        where TProjection : notnull
        where TQuery : notnull, IQuery<TProjection>;
}
