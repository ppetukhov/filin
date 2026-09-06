using Filin;

[assembly: ApplicationPart("Filin.Executor")]
namespace Filin;

/// <inheritdoc cref="IMediator"/>
sealed partial class Mediator(HandlersFactory executorFactory) : IMediator
{
    /// <inheritdoc/>
    public Task<ExecuteResult<TState>> ExecuteAsync<TCommand, TState>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull, ICommand<TState>
        where TState : notnull
    {
        var handler = executorFactory.CreateCommandHandler<TCommand, TState>();

        return handler.ExecuteAsync(command, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TProjection?> QueryAsync<TQuery, TProjection>(TQuery query, CancellationToken cancellationToken)
        where TQuery : notnull, IQuery<TProjection>
        where TProjection : notnull
    {
        var handler = executorFactory.CreateQueryHandler<TQuery, TProjection>();

        return handler.QueryAsync(query, cancellationToken);
    }
}
