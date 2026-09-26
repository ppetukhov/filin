using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

using Filin.DataFlow;
using Filin.Storage;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Internal;

sealed class StateManager<TState> where TState : notnull
{
    const int MAX_ATTEMPTS_COUNT = 3;
    readonly TransformBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> _loadStateBlock;
    readonly IEventStore _eventStore;
    readonly IAppendEventsBlock _appendEventsBlock;
    readonly IProjectorProxy<TState> _projector;
    readonly LRUCache<ProjectionId, Task<Projection?>> _cache;
    readonly IProjectionStore? _projectionStore;
    readonly ProjectionSavePipeline<(Projection projection, bool create)>? _pipeline;

    public StateManager(IEventStore eventStore,
        IAppendEventsBlock appendEventsBlock,
        IProjectorProxy<TState> projector,
        LRUCache<ProjectionId, Task<Projection?>> cache,
        IProjectionStore? projectionStore,
        ProjectionSavePipeline<ProjectionTuple>? pipeline = null
    )
    {
        _eventStore = eventStore;
        _appendEventsBlock = appendEventsBlock;
        _projector = projector;
        _cache = cache;
        _projectionStore = projectionStore;
        _pipeline = pipeline;
        _loadStateBlock = new(LoadStateAsync);
    }

    internal IAppendEventsBlock AppendEventsBlock => _appendEventsBlock;

    internal IPropagatorBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> LoadStateBlock => _loadStateBlock;

    internal IProjectorProxy<TState> Projector => _projector;

    public void Save(TState state, ProjectionId projectionId, ref StateMetadata metadata)
    {
        if (metadata.Status != StateStatus.Unchanged)
        {
            Debug.Assert(state is not null);

            var projection = new Projection
            {
                Id = projectionId,
                Position = metadata.Position,
                Timestamp = metadata.Timestamp,
                Payload = state
            };

            var cacheEntry = Task.FromResult<Projection?>(projection);

            _cache.Set(projectionId, cacheEntry);

            if (_pipeline is not null)
            {
                _ = _pipeline.TryWrite((projection, metadata.Status == StateStatus.Created ? true : false));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyEvent([NotNull] ref TState? state, ref StateMetadata metadata, ref DcbEventSequenced @event)
    {
        if (state is null)
        {
            state = _projector.Project(@event);
            metadata.Status = StateStatus.Created;
        }
        else
        {
            state = _projector.Project(state, @event);

            if (metadata.Status != StateStatus.Created)
            {
                metadata.Status = StateStatus.Updated;
            }
        }

        metadata.Position = @event.Position;
        metadata.Timestamp = @event.Timestamp;
    }

    Task<Projection?> CacheItemFactory(ProjectionId projectionId) => _projectionStore is not null ? _projectionStore.LoadAsync(projectionId, default) : Task.FromResult<Projection?>(null);

    Task<Projection?> UpdateCacheItem(ProjectionId projectionId, Task<Projection?> cacheEntry) => cacheEntry.IsFaulted ? CacheItemFactory(projectionId) : cacheEntry;

    async Task<LoadStateResponse<TState>> LoadStateAsync(LoadStateRequest<TState> request)
    {
        if (request.CancellationToken.IsCancellationRequested)
        {
            _ = request.TrySetCanceled();

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
                    projection = projectionTask.Result;
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
                catch
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
        catch (Exception ex)
        {
            _ = request.TrySetException(ex);

            return new LoadStateResponse<TState>(default, default, request);
        }
    }
}
