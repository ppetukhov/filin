using System.Threading.Tasks.Dataflow;

using Filin.Internal;

namespace Filin;

sealed class QueryHandler<TQuery, TState> : IDisposable
    where TState : notnull
    where TQuery : notnull, IQuery<TState>
{
    readonly IDisposable _disposable;
    readonly StateManager<TState> _stateManager;
    bool _disposed;

    public QueryHandler(StateManager<TState> stateManager)
    {
        _stateManager = stateManager;

        var execOpt = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true, EnsureOrdered = false, MaxDegreeOfParallelism = Environment.ProcessorCount };
        var linkOpt = new DataflowLinkOptions { PropagateCompletion = true };

        _disposable = stateManager.LoadStateBlock.LinkTo(new ActionBlock<LoadStateResponse<TState>>(OnLoaded, execOpt), linkOpt, IsOwnedResponse);
    }

    public Task<TState?> QueryAsync(TQuery query, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tags = query.GetTags();

        var q = _stateManager.Projector.GetQuery(tags);

        var projectionId = _stateManager.Projector.GetProjectionId(tags);

        var tcs = new TaskCompletionSource<TState?>();

        var request = new LoadStateRequest(this, projectionId, q, tcs, cancellationToken);

        var posted = _stateManager.LoadStateBlock.Post(request);

        if (!posted)
        {
            var ex = new MediatorException("System too busy");

            _ = tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposable.Dispose();

        _disposed = true;
    }

    void OnLoaded(LoadStateResponse<TState> response)
    {
        var loadRequest = (LoadStateRequest)response.LoadRequest;

        _ = loadRequest.TrySetResult(response.State);

        if (response.State is not null)
        {
            _ = _stateManager.SaveStateBlock.Post(new(response.State, loadRequest.ProjectionId, response.Metadata));
        }
    }

    bool IsOwnedResponse(LoadStateResponse<TState> resp) => resp.LoadRequest is LoadStateRequest req && ReferenceEquals(req.Owner, this);

    sealed class LoadStateRequest(
        QueryHandler<TQuery, TState> owner,
        ProjectionId projectionId,
        DcbQuery query,
        TaskCompletionSource<TState?> tcs,
        CancellationToken cancellationToken) : LoadStateRequest<TState>(projectionId, query)
    {
        public QueryHandler<TQuery, TState> Owner => owner;
        public override CancellationToken CancellationToken => cancellationToken;

        public bool TrySetResult(TState? result) => tcs.TrySetResult(result);
        public override bool TrySetCanceled(CancellationToken cancellationToken) => tcs.TrySetCanceled(cancellationToken);
        public override bool TrySetException(Exception exception) => tcs.TrySetException(exception);
    }
}
