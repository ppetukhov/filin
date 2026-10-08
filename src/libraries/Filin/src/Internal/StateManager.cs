using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

using Filin.Storage;
using Filin.Storage.DataFlow;

namespace Filin.Internal;

sealed class StateManager<TState> : IDisposable
    where TState : notnull
{
    const int MAX_ATTEMPTS_COUNT = 3;

    readonly BufferBlock<SaveStateRequest<TState>> _bufferBlock = new(new DataflowBlockOptions { EnsureOrdered = false });
    readonly TransformBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> _loadStateBlock;
    readonly IEventStore _eventStore;
    readonly LRUCache<ProjectionId, Task<Projection?>> _cache;
    readonly IProjectionStore? _projectionStore;
    readonly List<IDisposable> _disposable;
    bool _disposed;

    public StateManager(IEventStore eventStore,
        IAppendEventsBlock appendEventsBlock,
        IProjectorProxy<TState> projector,
        LRUCache<ProjectionId, Task<Projection?>> cache,
        SaveProjectionBlock saveProjectionBlock,
        IProjectionStore? projectionStore
    )
    {
        _eventStore = eventStore;
        AppendEventsBlock = appendEventsBlock;
        Projector = projector;
        _cache = cache;
        _projectionStore = projectionStore;
        _loadStateBlock = new(LoadStateAsync);

        var execOpt = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true, EnsureOrdered = false, MaxDegreeOfParallelism = Environment.ProcessorCount };

        var saveStateBlock = new TransformBlock<SaveStateRequest<TState>, Projection>(request =>
        {
            var (state, projectionId, metadata) = request;

            var projection = new Projection
            {
                Id = projectionId,
                Position = metadata.Position,
                Timestamp = metadata.Timestamp,
                Payload = state
            };

            var cacheEntry = Task.FromResult<Projection?>(projection);

            _cache.Set(projectionId, cacheEntry);

            return projection;
        },
        execOpt);

        var dropUnchanged = new ActionBlock<SaveStateRequest<TState>>(Drop, execOpt);

        var linkOpt = new DataflowLinkOptions { PropagateCompletion = true };

        _disposable =
        [
            _bufferBlock.LinkTo(dropUnchanged, linkOpt, IsUnchanged),
            _bufferBlock.LinkTo(saveStateBlock, linkOpt),
            saveStateBlock.LinkTo(saveProjectionBlock),
        ];
    }

    void Drop(SaveStateRequest<TState> _) { }

    bool IsUnchanged(SaveStateRequest<TState> request) => request.Metadata.Status == StateStatus.Unchanged;

    internal IAppendEventsBlock AppendEventsBlock { get; }

    internal IPropagatorBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> LoadStateBlock => _loadStateBlock;

    internal ITargetBlock<SaveStateRequest<TState>> SaveStateBlock => _bufferBlock;

    internal IProjectorProxy<TState> Projector { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyEvent([NotNull] ref TState? state, ref StateMetadata metadata, ref DcbEventSequenced @event)
    {
        if (state is null)
        {
            state = Projector.Project(@event);
            metadata.Status = StateStatus.Created;
        }
        else
        {
            state = Projector.Project(state, @event);

            if (metadata.Status != StateStatus.Created)
            {
                metadata.Status = StateStatus.Updated;
            }
        }

        metadata.Position = @event.Position;
        metadata.Timestamp = @event.Timestamp;
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

        _disposed = true;
    }

    Task<Projection?> CacheItemFactory(ProjectionId projectionId) => _projectionStore is not null ? _projectionStore.LoadAsync(projectionId, default) : Task.FromResult<Projection?>(null);

    Task<Projection?> UpdateCacheItem(ProjectionId projectionId, Task<Projection?> cacheEntry) => cacheEntry.IsFaulted ? CacheItemFactory(projectionId) : cacheEntry;

    async Task<LoadStateResponse<TState>> LoadStateAsync(LoadStateRequest<TState> request)
    {
        if (request.CancellationToken.IsCancellationRequested)
        {
            _ = request.TrySetCanceled(request.CancellationToken);

            return new LoadStateResponse<TState>(default, default, request);
        }

        try
        {
            Projection? projection = null;
            var attempts = 0;
            do
            {
                attempts++;
                var projectionTask = _cache.GetOrUpdate(request.ProjectionId, CacheItemFactory, UpdateCacheItem);

                if (projectionTask.IsCompletedSuccessfully)
                {
#pragma warning disable CA1849 // Call async methods when in an async method
                    projection = projectionTask.Result;
#pragma warning restore CA1849 // Call async methods when in an async method
                    break;
                }

                if (projectionTask.IsFaulted)
                {
                    continue;
                }

                try
                {
                    projection = await projectionTask.WaitAsync(request.CancellationToken).ConfigureAwait(false);
                    break;
                }
#pragma warning disable CA1031 // Modify 'LoadStateAsync' to catch a more specific allowed exception type, or rethrow the exception
                catch
#pragma warning restore CA1031 // Modify 'LoadStateAsync' to catch a more specific allowed exception type, or rethrow the exception
                {
                    continue;
                }
            } while (attempts < MAX_ATTEMPTS_COUNT);

            var (state, metadata) = projection is not null ? ((TState)projection.Payload, new StateMetadata(projection.Position, projection.Timestamp, StateStatus.Unchanged)) : default;

            await foreach (var evt in _eventStore.ReadAsync(request.Query, metadata.Position).WithCancellation(request.CancellationToken).ConfigureAwait(false))
            {
                var @event = evt;

                ApplyEvent(ref state, ref metadata, ref @event);
            }

            return new LoadStateResponse<TState>(state, metadata, request);
        }
#pragma warning disable CA1031 // Modify 'LoadStateAsync' to catch a more specific allowed exception type, or rethrow the exception
        catch (Exception ex)
#pragma warning restore CA1031 // Modify 'LoadStateAsync' to catch a more specific allowed exception type, or rethrow the exception
        {
            _ = request.TrySetException(ex);

            return new LoadStateResponse<TState>(default, default, request);
        }
    }
}

sealed record SaveStateRequest<TState>(TState State, ProjectionId ProjectionId, StateMetadata Metadata)
    where TState : notnull;
