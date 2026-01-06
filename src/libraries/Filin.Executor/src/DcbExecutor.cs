using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

using Filin;
using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Options;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

[assembly: ApplicationPart("Filin.Executor")]
namespace Filin;

#pragma warning disable CA1812
/// <inheritdoc cref="IDcbExecutor"/>
sealed class DcbExecutor(
    IEventStore eventStore,
    IOptions<ExecutorOptions> options,
    ProjectionSavePipeline<ProjectionTuple>? pipeline = null,
    IProjectionStore? projectionStore = null) : IDcbExecutor
#pragma warning restore CA1812
{
    const int MAX_RETRIES = 10;
    readonly ConcurrentDictionary<Type, object> _projectorInvokers = [];
    readonly ConcurrentDictionary<(Type Command, Type State), object> _deciderInvokers = [];
    readonly LRUCache<ProjectionId, Projection> _cache = new(255, TimeSpan.FromMinutes(15));

    /// <inheritdoc/>
    public async Task<TState?> ExecuteAsync<TCommand, TState>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : notnull, ICommand<TState>
        where TState : notnull
    {
        var projectorInvoker = GetProjectorInvoker<TState>();

        var tags = command.GetTags();

        var query = projectorInvoker.GetQuery(tags);

        var projectionId = projectorInvoker.GetProjectionId(tags);

        var (state, position, _, status) = await LoadStateAsync(projectionId, query, projectorInvoker, eventStore, _cache, projectionStore, cancellationToken).ConfigureAwait(false);

        var decider = GetDeciderInvoker<TCommand, TState>();

        var retries = 0;

        while (true)
        {
            DcbEvent[]? rentedBuff = null;

            try
            {
                var decideResult = state is null ? decider.Decide(command) : decider.Decide(command, state);

                if (decideResult.IsFailure)
                {
                    var err = decideResult.Error;

                    throw new InvalidOperationException($"decider [{err.Code}] {err.Message}");
                }

                DcbEventSequenced lastAppliedEvent = default;

                if (decideResult is { Count: 1 })
                {
                    var dcbEvent = decideResult[0];
                    position = await eventStore.AppendAsync(dcbEvent, query, position, cancellationToken).ConfigureAwait(false);
                    lastAppliedEvent = new DcbEventSequenced(dcbEvent, position);

                    state = state is not null ? projectorInvoker.Project(state, lastAppliedEvent) : projectorInvoker.Project(lastAppliedEvent);
                }
                else
                {
                    ArraySegment<DcbEvent> events;

                    {
                        rentedBuff = ArrayPool<DcbEvent>.Shared.Rent(decideResult.Count);

                        var idx = 0;

                        foreach (var evt in decideResult)
                        {
                            rentedBuff[idx++] = evt;
                        }

                        events = new ArraySegment<DcbEvent>(rentedBuff, 0, decideResult.Count);
                    }

                    var positions = await eventStore.AppendAsync(events, query, position, cancellationToken).ConfigureAwait(false);

                    var i = 0;

                    foreach (var pos in positions)
                    {
                        Debug.Assert(i < events.Count);

                        lastAppliedEvent = new DcbEventSequenced(events[i++], pos);

                        state = state is not null ? projectorInvoker.Project(state, lastAppliedEvent) : projectorInvoker.Project(lastAppliedEvent);
                        position = pos;
                    }
                }

                Debug.Assert(state is not null);

                var projection = new Projection
                {
                    Id = projectionId,
                    Position = lastAppliedEvent.Position,
                    Timestamp = lastAppliedEvent.Event.Timestamp,
                    Payload = state
                };

                _cache.Set(projectionId, projection);
                _ = pipeline?.TryWrite((projection, status != StateStatus.Updated ? true : false));

                return state;
            }
            catch (DcbConflictException) when (++retries < MAX_RETRIES)
            {
                // retry on conflict.
                (state, var lastEvent) = await ApplyLastEvents(state, position, query, eventStore, projectorInvoker, cancellationToken).ConfigureAwait(false);

                if (!lastEvent.Position.IsDefault)
                {
                    Debug.Assert(state is not null);

                    position = lastEvent.Position;
                }
                else
                {
                    throw new UnreachableException();
                }
            }
            finally
            {
                if (rentedBuff is not null)
                {
                    ArrayPool<DcbEvent>.Shared.Return(rentedBuff);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <inheritdoc/>
    public async Task<TProjection?> QueryAsync<TQuery, TProjection>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : notnull, IQuery<TProjection>
        where TProjection : notnull
    {
        var projectorInvoker = GetProjectorInvoker<TProjection>();

        var tags = query.GetTags();

        var q = projectorInvoker.GetQuery(tags);

        var projectionId = projectorInvoker.GetProjectionId(tags);

        var (state, position, timestamp, status) = await LoadStateAsync(projectionId, q, projectorInvoker, eventStore, _cache, projectionStore, cancellationToken).ConfigureAwait(false);

        if (pipeline is not null && status != StateStatus.None)
        {
            var projection = new Projection
            {
                Id = projectionId,
                Position = position,
                Timestamp = timestamp,
                Payload = state
            };

            _cache.Set(projectionId, projection);
            _ = pipeline.TryWrite((projection, status == StateStatus.Created ? true : false));
        }

        return state;
    }

    IDeciderInvoker<TCommand, TState> GetDeciderInvoker<TCommand, TState>() where TCommand : notnull, ICommand<TState> where TState : notnull =>
        (IDeciderInvoker<TCommand, TState>)_deciderInvokers.GetOrAdd((typeof(TCommand), typeof(TState)), CreateDeciderInvokerInstance);

    IProjectorInvoker<TState> GetProjectorInvoker<TState>() where TState : notnull =>
        (IProjectorInvoker<TState>)_projectorInvokers.GetOrAdd(typeof(TState), CreateProjectorInvokerInstance);

    object CreateDeciderInvokerInstance((Type CommandType, Type StateType) key) =>
        !options.Value.DeciderInvokerTypes.TryGetValue(key, out var invokerType)
            ? throw new InvalidOperationException($"Not found decider invoker type for command and state type ({key.CommandType.FullName}, {key.StateType.FullName})")
            : Activator.CreateInstance(invokerType, nonPublic: true) ?? throw new InvalidOperationException($"Fail create instance of {invokerType.FullName}");

    object CreateProjectorInvokerInstance(Type stateType) =>
        !options.Value.ProjectorInvokerTypes.TryGetValue(stateType, out var invokerType)
            ? throw new InvalidOperationException($"Not found projector invoker type for state type {stateType.FullName}")
            : Activator.CreateInstance(invokerType, nonPublic: true) ?? throw new InvalidOperationException($"Fail create instance of {invokerType.FullName}");

    static async Task<(TState state, EventPosition position, DateTimeOffset timestamp, StateStatus status)> LoadStateAsync<TState>(
        ProjectionId projectionId,
        DcbQuery query,
        IProjectorInvoker<TState> projectorInvoker,
        IEventStore eventStore,
        LRUCache<ProjectionId, Projection> cache,
        IProjectionStore? projectionStore,
        CancellationToken cancellationToken) where TState : notnull
    {
        if (!cache.TryGet(projectionId, out var projection))
        {
            projection = projectionStore is not null ? await projectionStore.LoadAsync(projectionId, cancellationToken).ConfigureAwait(false) : null;
        }

        var position = projection is null ? default : projection.Position;
        var state = projection is null ? default : (TState)projection.Payload;

        (state, var lastEvent) = await ApplyLastEvents(state, position, query, eventStore, projectorInvoker, cancellationToken).ConfigureAwait(false);

        if (!lastEvent.Position.IsDefault)
        {
            Debug.Assert(state is not null);

            return (state, lastEvent.Position, lastEvent.Event.Timestamp, projection is null ? StateStatus.Created : StateStatus.Updated);
        }

        return projection is not null
            ? ((TState)projection.Payload, projection.Position, projection.Timestamp, default)
            : default;
    }

    static async Task<(TState? state, DcbEventSequenced lastEvent)> ApplyLastEvents<TState>(
        TState? state,
        EventPosition afterPosition,
        DcbQuery failIfEventsMatch,
        IEventStore eventStore,
        IProjectorInvoker<TState> projectorInvoker,
        CancellationToken cancellationToken) where TState : notnull
    {
        DcbEventSequenced lastEvent = default;

        await foreach (var evt in eventStore.ReadAsync(failIfEventsMatch, afterPosition).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            state = state is not null ? projectorInvoker.Project(state, evt) : projectorInvoker.Project(evt);
            lastEvent = evt;
        }

        return (state, lastEvent);
    }

    enum StateStatus
    {
        None = 0,
        Created,
        Updated,
    }
}
