using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Configuration;
using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Options;

using ProjectionTuple = (Filin.Storage.Projection projection, bool create);

namespace Filin;

[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
sealed class HandlersFactory(
    IEventStore eventStore,
    IOptions<ExecutorOptions> options,
    LRUCache<ProjectionId, Task<Projection?>> _cache,
    IProjectionStore? projectionStore = null,
    ProjectionSavePipeline<ProjectionTuple>? pipeline = null)
{
    const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    readonly ConcurrentDictionary<Type, object> _stateManagers = [];
    readonly ConcurrentDictionary<(Type Command, Type State), object> _commandHandlers = [];
    readonly ConcurrentDictionary<(Type QueryType, Type State), object> _queryHandlers = [];

    public CommandHandler<TCommand, TState> CreateCommandHandler<TCommand, TState>() where TCommand : notnull, ICommand<TState> where TState : notnull =>
        (CommandHandler<TCommand, TState>)_commandHandlers.GetOrAdd((typeof(TCommand), typeof(TState)), CreateCommandHandler);

    public QueryHandler<TQuery, TProjection> CreateQueryHandler<TQuery, TProjection>() where TQuery : notnull, IQuery<TProjection>
        where TProjection : notnull => (QueryHandler<TQuery, TProjection>)_queryHandlers.GetOrAdd((typeof(TQuery), typeof(TProjection)), CreateQueryHandler);

    object CreateQueryHandler((Type QueryType, Type StateType) key)
    {
        var manager = GetStateManager(key.StateType);

        var type = typeof(QueryHandler<,>).MakeGenericType(key.QueryType, key.StateType);

        return Activator.CreateInstance(type, ConstructorDefault, null, [manager], null) ?? throw new InvalidOperationException($"Fail create instance of {type.FullName}");
    }

    object CreateCommandHandler((Type CommandType, Type StateType) key)
    {
        var decider = CreateDeciderInvokerInstance(key);
        var manager = GetStateManager(key.StateType);

        var type = typeof(CommandHandler<,>).MakeGenericType(key.CommandType, key.StateType);

        return Activator.CreateInstance(type, ConstructorDefault, null, [decider, manager], null) ?? throw new InvalidOperationException($"Fail create instance of {type.FullName}");
    }

    object GetStateManager(Type stateType) => _stateManagers.GetOrAdd(stateType, CreateStateManagerInstance);

    object CreateStateManagerInstance(Type stateType)
    {
        var type = typeof(StateManager<>).MakeGenericType(stateType);

        var projector = CreateProjectorInvokerInstance(stateType);

        return Activator.CreateInstance(type, ConstructorDefault, null, [eventStore, projector, _cache, projectionStore, pipeline], null) ?? throw new InvalidOperationException($"Fail create instance of {type.FullName}");
    }

    object CreateProjectorInvokerInstance(Type stateType) =>
        !options.Value.ProjectorProxyTypes.TryGetValue(stateType, out var invokerType)
            ? throw new InvalidOperationException($"Not found projector invoker type for state type {stateType.FullName}")
            : Activator.CreateInstance(invokerType, nonPublic: true) ?? throw new InvalidOperationException($"Fail create instance of {invokerType.FullName}");

    object CreateDeciderInvokerInstance((Type CommandType, Type StateType) key) =>
        !options.Value.DeciderProxyTypes.TryGetValue(key, out var invokerType)
            ? throw new InvalidOperationException($"Not found decider invoker type for command and state type ({key.CommandType.FullName}, {key.StateType.FullName})")
            : Activator.CreateInstance(invokerType, nonPublic: true) ?? throw new InvalidOperationException($"Fail create instance of {invokerType.FullName}");
}
