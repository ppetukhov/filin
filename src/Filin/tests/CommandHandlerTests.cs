using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Logging.Abstractions;

namespace Filin.Tests;

public class CommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange - Instantiate decider proxy, projector proxy, and state manager
        var decider = new DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>();
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: new StubEventStore(),
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        var commandHandler = new CommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>(decider, stateManager);

        // Act - Dispose the command handler instance
        commandHandler.Dispose();

        // Assert - Executing a command on a disposed handler must throw ObjectDisposedException
        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            commandHandler.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoadStateBlockCompleted_ShouldThrowMediatorException()
    {
        // Arrange - Setup CommandHandler dependencies
        var decider = new DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>();
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: new StubEventStore(),
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        using var commandHandler = new CommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>(decider, stateManager);

        // Complete LoadStateBlock to simulate channel closure/capacity saturation (Post returns false)
        stateManager.LoadStateBlock.Complete();

        // Act & Assert - Verify that MediatorException ("System too busy") is thrown
        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());
        var ex = await Assert.ThrowsAsync<MediatorException>(() =>
            commandHandler.ExecuteAsync(command, CancellationToken.None));

        Assert.Equal("System too busy", ex.Message);
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldBeIdempotent()
    {
        // Arrange - Setup CommandHandler instance
        var decider = new DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>();
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
#pragma warning disable CA2000 // Dispose objects before losing scope
        var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);
#pragma warning restore CA2000 // Dispose objects before losing scope

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: new StubEventStore(),
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        var commandHandler = new CommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>(decider, stateManager);

        // Act & Assert - Multiple Dispose calls should complete without throwing exceptions
        commandHandler.Dispose();
        commandHandler.Dispose();
    }
}
