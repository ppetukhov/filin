using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Storage;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Internal;

sealed class StateManager<TState>(IEventStore eventStore,
    IProjectorProxy<TState> projector,
    LRUCache<ProjectionId, Task<Projection?>> cache,
    IProjectionStore? projectionStore,
    ProjectionSavePipeline<ProjectionTuple>? pipeline = null
    ) where TState : notnull
{
    public IEventStore EventStore => eventStore;
    public IProjectorProxy<TState> Projector => projector;

    Task<Projection?> CacheItemFactory(ProjectionId projectionId) => projectionStore is not null ? projectionStore.LoadAsync(projectionId, default) : Task.FromResult<Projection?>(null);

    Task<Projection?> UpdateCacheItem(ProjectionId projectionId, Task<Projection?> cacheEntry) => cacheEntry.IsFaulted ? CacheItemFactory(projectionId) : cacheEntry;

    public async Task<(TState? state, StateMetadata metadata)> LoadStateAsync(
        ProjectionId projectionId,
        DcbQuery query,
        CancellationToken cancellationToken)
    {
        Projection? projection = null;
        var attempts = 0;
        do
        {
            attempts++;
            var projectionTask = cache.GetOrUpdate(projectionId, CacheItemFactory, UpdateCacheItem);

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
                projection = await projectionTask.ConfigureAwait(false);
                break;
            }
            catch
            {
                continue;
            }
        } while (attempts < 3);

        var (state, metadata) = projection is not null ? ((TState)projection.Payload, new StateMetadata(projection.Position, projection.Timestamp, StateStatus.Unchanged)) : default;

        await foreach (var evt in eventStore.ReadAsync(query, metadata.Position).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var @event = evt;

            ApplyEvent(ref state, ref metadata, ref @event);
        }

        return (state, metadata);
    }

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

            cache.Set(projectionId, cacheEntry);

            if (pipeline is not null)
            {
                _ = pipeline.TryWrite((projection, metadata.Status == StateStatus.Created ? true : false));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyEvent([NotNull] ref TState? state, ref StateMetadata metadata, ref DcbEventSequenced @event)
    {
        if (state is null)
        {
            state = projector.Project(@event);
            metadata.Status = StateStatus.Created;
        }
        else
        {
            state = projector.Project(state, @event);

            if (metadata.Status != StateStatus.Created)
            {
                metadata.Status = StateStatus.Updated;
            }
        }

        metadata.Position = @event.Position;
        metadata.Timestamp = @event.Timestamp;
    }
}
