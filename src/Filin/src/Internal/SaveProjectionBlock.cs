using System.Threading.Tasks.Dataflow;

using Filin.Storage;

using Microsoft.Extensions.Logging;

namespace Filin.Internal;

sealed partial class SaveProjectionBlock : ITargetBlock<Projection>, IDisposable
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

        const int batchSize = 10;
        var flushPeriod = TimeSpan.FromSeconds(5);

        _batchBufferBlock = new BatchBlock<Projection>(batchSize);

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
        if (_disposed)
        {
            return;
        }

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
#pragma warning disable CA1031 // Modify 'SaveBatchAsync' to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception ex)
#pragma warning restore CA1031 // Modify 'SaveBatchAsync' to catch a more specific allowed exception type, or rethrow the exception
            {
                LogSaveBatchFailed(_logger, ex);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        EventName = "SaveBatchFailed",
        Message = "Failed to save batch projections"
    )]
    static partial void LogSaveBatchFailed(ILogger logger, Exception ex);
}
