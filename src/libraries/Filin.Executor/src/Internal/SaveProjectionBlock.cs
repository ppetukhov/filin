using System.Threading.Tasks.Dataflow;

using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Logging;

namespace Filin.Internal;

sealed class SaveProjectionBlock : ISaveProjectionBlock, IDisposable
{
    readonly BatchBlock<Projection> _batchBufferBlock;
    readonly IProjectionStore? _projectionStore;
    readonly ILogger _logger;
    readonly List<IDisposable> _disposable;
    bool _disposed;

    Task IDataflowBlock.Completion => _batchBufferBlock.Completion;

    public SaveProjectionBlock(
        ILoggerFactory loggerFactory,
        IProjectionStore? projectionStore,
        TimeProvider? timeProvider)
    {
        _logger = loggerFactory.CreateLogger("Filin.SaveProjectionBlock");
        _projectionStore = projectionStore;

        const int BATCH_SIZE = 10;
        var flushPeriod = TimeSpan.FromSeconds(5);

        _batchBufferBlock = new BatchBlock<Projection>(BATCH_SIZE);

        var saveBatchBlock = new ActionBlock<Projection[]>(SaveBatchAsync, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });

        var flushTimer = (timeProvider ?? TimeProvider.System).CreateTimer(state =>
        {
            if (state is BatchBlock<Projection> buff && !buff.Completion.IsCompleted)
            {
                buff.TriggerBatch();
            }
        },
        _batchBufferBlock,
        flushPeriod,
        flushPeriod);

        _disposable =
        [
            _batchBufferBlock.LinkTo(saveBatchBlock, new DataflowLinkOptions { PropagateCompletion = true }),
            flushTimer
        ];
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var item in _disposable)
        {
            item.Dispose();
        }

        _disposable.Clear();

        _disposed = true;
    }

    DataflowMessageStatus ITargetBlock<Projection>.OfferMessage(DataflowMessageHeader messageHeader, Projection messageValue, ISourceBlock<Projection>? source, bool consumeToAccept)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ((ITargetBlock<Projection>)_batchBufferBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    void IDataflowBlock.Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _batchBufferBlock.Complete();
    }

    void IDataflowBlock.Fault(Exception exception)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ((ITargetBlock<Projection>)_batchBufferBlock).Fault(exception);
    }

    async Task SaveBatchAsync(Projection[] projections)
    {
        if (projections is { Length: > 0 } && _projectionStore is not null && !_disposed)
        {
            try
            {
                if (projections.Length == 1)
                {
                    await _projectionStore.SaveAsync(projections[0], default).ConfigureAwait(false);
                }
                else
                {
                    await _projectionStore.SaveAsync(projections, default).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(exception: ex, message: "Failed to save batch projections");
                }
            }
        }
    }
}
