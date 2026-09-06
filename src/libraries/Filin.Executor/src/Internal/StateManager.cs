using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Storage;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin.Internal;

sealed class StateManager<TState>(IEventStore eventStore,
    IProjectorInvoker<TState> projector,
    LRUCache<ProjectionId, Projection> cache,
    IProjectionStore? projectionStore,
    ProjectionSavePipeline<ProjectionTuple>? pipeline = null
    ) where TState : notnull
{
    public IEventStore EventStore => eventStore;
    public IProjectorInvoker<TState> Projector => projector;

    public async Task<(TState? state, StateMetadata metadata)> LoadStateAsync(
        ProjectionId projectionId,
        DcbQuery query,
        CancellationToken cancellationToken)
    {
        if (!cache.TryGet(projectionId, out var projection))
        {
            projection = projectionStore is not null ? await projectionStore.LoadAsync(projectionId, cancellationToken).ConfigureAwait(false) : null;
        }

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

            cache.Set(projectionId, projection);

            if (pipeline is not null)
            {
                _ = pipeline.TryWrite((projection, metadata.Status == StateStatus.Created ? true : false));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyEvent([NotNull] ref TState? state, ref StateMetadata metadata, ref DcbEventSequenced @event)
    {
        state = state is not null ? projector.Project(state, @event) : projector.Project(@event);

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
        metadata.Timestamp = @event.Event.Timestamp;
    }
}
