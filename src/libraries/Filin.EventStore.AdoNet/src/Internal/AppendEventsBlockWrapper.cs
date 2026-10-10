using System.Threading.Tasks.Dataflow;

using Filin.Storage;
using Filin.Storage.DataFlow;

namespace Filin.EventStore.AdoNet.Internal;

sealed class AppendEventsBlockWrapper : IDisposable
{
    readonly IAppendEventsBlock _appendEventsBlock;
    IDisposable? _disp;
    bool _disposed;

    internal AppendEventsBlockWrapper(IAppendEventsBlock appendEventsBlock)
    {
        ArgumentNullException.ThrowIfNull(appendEventsBlock);

        var linkOption = new DataflowLinkOptions { PropagateCompletion = true };

        var execSingleOpt = new ExecutionDataflowBlockOptions
        {
            EnsureOrdered = false,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            SingleProducerConstrained = true
        };

        _disp = appendEventsBlock.LinkTo(new ActionBlock<AppendEventsResponse>(SetResult, execSingleOpt), linkOption, IsOwnedResponse);

        _appendEventsBlock = appendEventsBlock;
    }

    internal Task<AppendResult> AppendAventAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<AppendResult>();
        var context = new LocalContext(this, tcs, cancellationToken);

        var req = new AppendEventRequest(@event, appendCondition, context);

        var posted = _appendEventsBlock.Post(req);

        if (!posted)
        {
            _ = tcs.TrySetException(SystemIsToBusyException());
        }

        return tcs.Task;
    }

    internal Task<AppendResult> AppendEventsAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<AppendResult>();
        var context = new LocalContext(this, tcs, cancellationToken);

        var req = new AppendEventsRequest(events, appendCondition, context);

        var posted = _appendEventsBlock.Post(req);

        if (!posted)
        {
            _ = tcs.TrySetException(SystemIsToBusyException());
        }

        return tcs.Task;
    }

    static EventStoreException SystemIsToBusyException() => new("System is too busy.");

    static void SetResult(AppendEventsResponse resp) => ((LocalContext)resp.Context).TrySetResult(resp.Result);

    bool IsOwnedResponse(AppendEventsResponse resp) => resp.Context is LocalContext req && ReferenceEquals(this, req.Owner);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disp?.Dispose();
        _disp = null;

        _disposed = true;
    }

    sealed class LocalContext(AppendEventsBlockWrapper owner, TaskCompletionSource<AppendResult> tcs, CancellationToken cancellationToken) : IContext
    {
        public AppendEventsBlockWrapper Owner => owner;
        public bool IsCompleted => tcs.Task.IsCompleted;
        public CancellationToken CancellationToken => cancellationToken;

        public bool TrySetResult(AppendResult result) => tcs.TrySetResult(result);
        public bool TrySetCanceled(CancellationToken cancellationToken) => tcs.TrySetCanceled(cancellationToken);
        public bool TrySetException(Exception exception) => tcs.TrySetException(exception);
    }
}
