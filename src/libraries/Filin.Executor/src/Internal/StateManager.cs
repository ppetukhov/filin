using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;

using Filin.DataFlow;
using Filin.Storage;

using Microsoft.Extensions.Logging;

namespace Filin.Internal;

sealed class StateManager<TState> : IDisposable
    where TState : notnull
{
    const int MAX_ATTEMPTS_COUNT = 3;
    readonly TransformBlock<SaveStateRequest<TState>, (Projection?, bool)> _saveStateBlock;
    readonly TransformBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> _loadStateBlock;
    readonly IEventStore _eventStore;
    readonly IAppendEventsBlock _appendEventsBlock;
    readonly IProjectorProxy<TState> _projector;
    readonly LRUCache<ProjectionId, Task<Projection?>> _cache;
    readonly IProjectionStore? _projectionStore;
    readonly ILogger _logger;
    readonly List<IDisposable> _disposable;
    bool _disposed;

    public StateManager(IEventStore eventStore,
        IAppendEventsBlock appendEventsBlock,
        IProjectorProxy<TState> projector,
        LRUCache<ProjectionId, Task<Projection?>> cache,
        ILoggerFactory loggerFactory,
        IProjectionStore? projectionStore
    )
    {
        _logger = loggerFactory.CreateLogger("Filin.StateManager");
        _eventStore = eventStore;
        _appendEventsBlock = appendEventsBlock;
        _projector = projector;
        _cache = cache;
        _projectionStore = projectionStore;
        _loadStateBlock = new(LoadStateAsync);

        var saveBlock = new ActionBlock<(Projection?, bool)>(SaveAsync, new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });

        _saveStateBlock = new(request =>
        {
            var (state, projectionId, metadata) = request;

            if (metadata.Status == StateStatus.Unchanged)
            {
                return (default, default);
            }

            var projection = new Projection
            {
                Id = projectionId,
                Position = metadata.Position,
                Timestamp = metadata.Timestamp,
                Payload = state
            };

            var cacheEntry = Task.FromResult<Projection?>(projection);

            _cache.Set(projectionId, cacheEntry);

            return (projection, metadata.Status == StateStatus.Created);
        },
        new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

        _disposable =
        [
            _saveStateBlock.LinkTo(saveBlock, new DataflowLinkOptions { Append = false, PropagateCompletion = true }, static resp => resp.Item2), // created first
            _saveStateBlock.LinkTo(saveBlock, new DataflowLinkOptions { Append = true, PropagateCompletion = true }),
        ];
    }

    internal IAppendEventsBlock AppendEventsBlock => _appendEventsBlock;

    internal IPropagatorBlock<LoadStateRequest<TState>, LoadStateResponse<TState>> LoadStateBlock => _loadStateBlock;

    internal ITargetBlock<SaveStateRequest<TState>> SaveStateBlock => _saveStateBlock;

    internal IProjectorProxy<TState> Projector => _projector;

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

    async Task SaveAsync((Projection?, bool) request)
    {
        var (projection, created) = request;

        if (projection is not null && _projectionStore is not null)
        {
            try
            {
                await _projectionStore.SaveAsync(projection, created, default).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(exception: ex, message: "Failed to save projection");
                }
            }
        }
    }
}

sealed record SaveStateRequest<TState>(TState State, ProjectionId ProjectionId, StateMetadata Metadata)
    where TState : notnull;
