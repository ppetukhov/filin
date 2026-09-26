using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

using Filin.DataFlow;
using Filin.Storage;

namespace Filin.Internal;

sealed class CommandHandler<TCommand, TState> : IDisposable
    where TState : notnull
    where TCommand : notnull, ICommand<TState>
{
    const int MAX_CONFLICTS_COUNT = 10;
    readonly IDeciderProxy<TCommand, TState> _decider;
    readonly StateManager<TState> _stateManager;
    readonly List<IDisposable> _disposables;
    bool _disposed;

    public CommandHandler(IDeciderProxy<TCommand, TState> decider, StateManager<TState> stateManager)
    {
        _decider = decider;
        _stateManager = stateManager;

        var execOpt = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true, MaxDegreeOfParallelism = Environment.ProcessorCount };
        var linkOpt = new DataflowLinkOptions { PropagateCompletion = true };

        var decideBlock = new TransformBlock<LoadedState, DecideResponse>(Decide, execOpt);

        var toAppendEventRequestBlock = new TransformBlock<DecideResponse, AppendEventRequest>(ToAppendEventRequest, execOpt);

        var toAppendEventsRequestBlock = new TransformBlock<DecideResponse, AppendEventsRequest>(ToAppendEventsRequest, execOpt);

        var toRetryLoadStateBlock = new TransformBlock<AppendedEvents, LoadStateRequest<TState>>(static request => request.LoadedState.LoadStateResponse.AddConflict(), execOpt);

        var applyEventsBlock = new TransformBlock<AppendedEvents, (TState, ProjectionId, StateMetadata)>(ApplyEventsToState, execOpt);

        var stateLoadedBlock = new TransformBlock<LoadStateResponse<TState>, LoadedState>(ToLoadedState, execOpt);

        var appendedEventsBlock = new TransformBlock<AppendEventsResponse, AppendedEvents>(ToAppendedEvents, execOpt);

        _disposables =
        [
            toRetryLoadStateBlock.LinkTo(stateManager.LoadStateBlock),

            stateManager.LoadStateBlock.LinkTo(stateLoadedBlock, linkOpt, IsOwnedResponse),

            stateLoadedBlock.LinkTo(new ActionBlock<LoadedState>(Drop), linkOpt, IsCompleted),  // drop completed
            stateLoadedBlock.LinkTo(decideBlock, linkOpt),

            decideBlock.LinkTo(toAppendEventRequestBlock, linkOpt, static resp => resp.DecideResult.Count == 1),
            decideBlock.LinkTo(toAppendEventsRequestBlock, linkOpt, static resp => resp.DecideResult.IsSuccess),
            decideBlock.LinkTo(new ActionBlock<DecideResponse>(Drop, execOpt), linkOpt, IsCompleted), // drop completed

            toAppendEventRequestBlock.LinkTo(stateManager.AppendEventsBlock, linkOpt),
            toAppendEventsRequestBlock.LinkTo(stateManager.AppendEventsBlock, linkOpt),

            stateManager.AppendEventsBlock.LinkTo(appendedEventsBlock, linkOpt, IsOwnedResponse),

            appendedEventsBlock.LinkTo(applyEventsBlock, linkOpt, static resp => resp.Result.IsSuccess),
            appendedEventsBlock.LinkTo(toRetryLoadStateBlock, linkOpt, static resp => resp.Result.IsConflict),
            appendedEventsBlock.LinkTo(new ActionBlock<AppendedEvents>(DropCompletedOrError, execOpt), linkOpt, IsCompletedOrError), // drop error result

            applyEventsBlock.LinkTo(new ActionBlock<(TState, ProjectionId, StateMetadata)>(SaveState, execOpt), linkOpt)
        ];
    }

    public Task<ExecuteResult<TState>> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tags = command.GetTags();
        var query = _stateManager.Projector.GetQuery(tags);
        var projectionId = _stateManager.Projector.GetProjectionId(tags);

        var tcs = new TaskCompletionSource<ExecuteResult<TState>>();

        var request = new LoadStateRequest(this, 0, projectionId, query, command, cancellationToken, tcs);

        var posted = _stateManager.LoadStateBlock.Post(request);

        if (!posted)
        {
            var ex = new MediatorException("System too busy");

            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var item in _disposables)
        {
            item.Dispose();
        }

        _disposables.Clear();

        _disposed = true;
    }

    static void DropCompletedOrError(AppendedEvents append)
    {
        if (append.Result.IsError)
        {
            var ex = append.Result.Exception;

            Debug.Assert(ex is not null);

            append.LoadedState.LoadStateResponse.TrySetException(ex);
        }
        else
        {
            Debug.Assert(append.LoadedState.LoadStateResponse.IsCompleted);
        }
    }

    static void Drop(LoadedState _) { }
    static void Drop(DecideResponse _) { }

    static bool IsCompleted(LoadedState resp) => resp.LoadStateResponse.IsCompleted;
    static bool IsCompleted(DecideResponse resp) => IsCompleted(resp.LoadState);
    static bool IsCompletedOrError(AppendedEvents resp) => IsCompleted(resp.LoadedState);

    static AppendedEvents ToAppendedEvents(AppendEventsResponse resp) => new AppendedEvents(resp.Result, (LoadedState)resp.Context);

    static AppendEventRequest ToAppendEventRequest(DecideResponse request)
    {
        var (decideResult, loadedState) = request;
        var (_, metadata, loadStateRequest) = loadedState;

        var appendCondition = new AppendCondition(loadStateRequest.Query, metadata.Position);

        var @event = decideResult[0];

        return new AppendEventRequest(@event, appendCondition, loadedState, loadStateRequest.CancellationToken);
    }

    static AppendEventsRequest ToAppendEventsRequest(DecideResponse request)
    {
        var (decideResult, loadStateResponse) = request;
        var (_, metadata, loadStateRequest) = loadStateResponse;

        var appendCondition = new AppendCondition(loadStateRequest.Query, metadata.Position);

        var events = new DcbEvent[decideResult.Count];

        var idx = 0;

        foreach (var evt in decideResult)
        {
            events[idx++] = evt;
        }

        return new AppendEventsRequest(events, appendCondition, loadStateResponse, loadStateRequest.CancellationToken);
    }

    static LoadedState ToLoadedState(LoadStateResponse<TState> resp)
    {
        var (state, metadata, loadRequest) = resp;
        return new LoadedState(state, metadata, (LoadStateRequest)loadRequest);
    }

    bool IsOwnedResponse(LoadStateResponse<TState> resp) => resp.LoadRequest is LoadStateRequest req && ReferenceEquals(this, req.Owner);
    bool IsOwnedResponse(AppendEventsResponse resp) => resp.Context is LoadedState ctx && ReferenceEquals(ctx.LoadStateResponse.Owner, this);

    DecideResponse Decide(LoadedState loadedState)
    {
        var (state, _, loadStateRequest) = loadedState;

        if (loadStateRequest.ConflictCount >= MAX_CONFLICTS_COUNT)
        {
            var ex = new MediatorException($"Too many conflicts");

            loadStateRequest.TrySetException(ex);

            return new DecideResponse(default, loadedState);
        }

        try
        {
            var decideResult = state is null ? _decider.Decide(loadStateRequest.Command) : _decider.Decide(loadStateRequest.Command, state);

            if (decideResult.IsFailure)
            {
                loadStateRequest.TrySetResult(decideResult.Error);
            }

            return new DecideResponse(decideResult, loadedState);
        }
        catch (Exception ex)
        {
            loadStateRequest.TrySetException(ex);

            return new DecideResponse(default, loadedState);
        }
    }

    (TState, ProjectionId, StateMetadata) ApplyEventsToState(AppendedEvents request)
    {
        var (appendResult, (state, metadata, loadStateRequest)) = request;

        Debug.Assert(appendResult.IsSuccess);

        try
        {
            foreach (var evt in appendResult)
            {
                var @event = evt;

                _stateManager.ApplyEvent(ref state, ref metadata, ref @event);
            }

            Debug.Assert(state is not null);

            loadStateRequest.TrySetResult(state);

            return new(state, loadStateRequest.ProjectionId, metadata);
        }
        catch (Exception ex)
        {
            loadStateRequest.TrySetException(ex);

            return new(default!, default, default);
        }
    }

    void SaveState((TState, ProjectionId, StateMetadata) request)
    {
        var (state, projectionId, metadata) = request;

        if (state is not null)
        {
            _stateManager.Save(state, projectionId, ref metadata);
        }
    }

    readonly record struct AppendedEvents(AppendResult Result, LoadedState LoadedState);
    readonly record struct DecideResponse(DecideResult DecideResult, LoadedState LoadState);
    sealed record LoadedState(TState? State, StateMetadata Metadata, LoadStateRequest LoadStateResponse);

    sealed class LoadStateRequest(
        CommandHandler<TCommand, TState> owner,
        int conflictCount,
        ProjectionId projectionId,
        DcbQuery query,
        TCommand command,
        CancellationToken cancellationToken,
        TaskCompletionSource<ExecuteResult<TState>> tcs) : LoadStateRequest<TState>(projectionId, query, cancellationToken)
    {
        public CommandHandler<TCommand, TState> Owner => owner;
        public int ConflictCount => conflictCount;
        public TCommand Command => command;
        public bool IsCompleted => tcs.Task.IsCompleted;
        public LoadStateRequest AddConflict() => new LoadStateRequest(owner, conflictCount + 1, ProjectionId, Query, command, CancellationToken, tcs);
        public bool TrySetResult(ExecuteResult<TState> result) => tcs.TrySetResult(result);
        public override bool TrySetCanceled() => tcs.TrySetCanceled(CancellationToken);
        public override bool TrySetException(Exception exception) => tcs.TrySetException(exception);
    }
}
