namespace Filin;

/// <inheritdoc cref="IMediator"/>
sealed partial class Mediator(HandlersFactory handlersFactory) : IMediator
{
    /// <inheritdoc/>
    public Task<ExecuteResult<TState>> ExecuteAsync<TCommand, TState>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull, ICommand<TState>
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(command);

        var handler = handlersFactory.CreateCommandHandler<TCommand, TState>();

        return handler.ExecuteAsync(command, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TProjection?> QueryAsync<TQuery, TProjection>(TQuery query, CancellationToken cancellationToken)
        where TQuery : notnull, IQuery<TProjection>
        where TProjection : notnull
    {
        ArgumentNullException.ThrowIfNull(query);

        var handler = handlersFactory.CreateQueryHandler<TQuery, TProjection>();

        return handler.QueryAsync(query, cancellationToken);
    }
}
