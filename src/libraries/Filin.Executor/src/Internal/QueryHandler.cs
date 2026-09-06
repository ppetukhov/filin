namespace Filin.Internal;

sealed class QueryHandler<TQuery, TState>(StateManager<TState> stateManager)
    where TQuery : notnull, IQuery<TState>
    where TState : notnull
{
    public async Task<TState?> QueryAsync(TQuery query, CancellationToken cancellationToken)
    {
        var tags = query.GetTags();

        var q = stateManager.Projector.GetQuery(tags);

        var projectionId = stateManager.Projector.GetProjectionId(tags);

        var (state, metadata) = await stateManager.LoadStateAsync(projectionId, q, cancellationToken).ConfigureAwait(false);

        if (state is not null)
        {
            stateManager.Save(state, projectionId, ref metadata);
        }

        return state;
    }
}
