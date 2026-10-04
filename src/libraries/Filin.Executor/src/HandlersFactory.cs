using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Configuration;
using Filin.Internal;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Options;

namespace Filin;

[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
sealed class HandlersFactory(
    IEventStore eventStore,
    IAppendEventsBlock appendEventsBlock,
    IOptions<ExecutorOptions> options,
    LRUCache<ProjectionId, Task<Projection?>> _cache,
    ISaveProjectionBlock saveProjectionBlock,
    IProjectionStore? projectionStore = null) : IDisposable
{
    const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    readonly ConcurrentDictionary<Type, object> _stateManagers = [];
    readonly ConcurrentDictionary<(Type Command, Type State), object> _commandHandlers = [];
    readonly ConcurrentDictionary<(Type QueryType, Type State), object> _queryHandlers = [];
    bool _disposed;

    public CommandHandler<TCommand, TState> CreateCommandHandler<TCommand, TState>() where TCommand : notnull, ICommand<TState> where TState : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (CommandHandler<TCommand, TState>)_commandHandlers.GetOrAdd((typeof(TCommand), typeof(TState)), CreateCommandHandler);
    }

    public QueryHandler<TQuery, TProjection> CreateQueryHandler<TQuery, TProjection>() where TQuery : notnull, IQuery<TProjection>
        where TProjection : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (QueryHandler<TQuery, TProjection>)_queryHandlers.GetOrAdd((typeof(TQuery), typeof(TProjection)), CreateQueryHandler);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var item in _commandHandlers.Values)
        {
            if (item is IDisposable disp)
            {
                disp.Dispose();
            }
        }

        foreach (var item in _queryHandlers.Values)
        {
            if (item is IDisposable disp)
            {
                disp.Dispose();
            }
        }

        foreach (var item in _stateManagers.Values)
        {
            if (item is IDisposable disp)
            {
                disp.Dispose();
            }
        }

        _commandHandlers.Clear();
        _queryHandlers.Clear();
        _stateManagers.Clear();

        _disposed = true;
    }

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

        return Activator.CreateInstance(type, ConstructorDefault, null, [eventStore, appendEventsBlock, projector, _cache, saveProjectionBlock, projectionStore], null) ?? throw new InvalidOperationException($"Fail create instance of {type.FullName}");
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
