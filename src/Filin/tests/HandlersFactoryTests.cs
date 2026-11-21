using Filin.Configuration;
using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Filin.Tests;

public class HandlersFactoryTests
{
    [Fact]
    public void Dispose_ShouldReleaseCachedDisposables()
    {
        // Arrange
        var options = Options.Create(new FilinOptions());
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(10, TimeSpan.FromMinutes(1));
        var factory = new HandlersFactory(null!, new StubAppendEventsBlock(), options, cache, null!, null);

        // Act
        factory.Dispose();

        // Assert - Повторный вызов после Dispose должен выбрасывать ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(factory.CreateCommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>);
    }

    [Fact]
    public void CreateCommandHandler_ShouldCacheAndReturnSameInstance()
    {
        // Arrange - Create factory with configured options
        using var factory = CreateFactory();

        // Act - Request CommandHandler twice for the same command and state pair
        var handler1 = factory.CreateCommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>();
        var handler2 = factory.CreateCommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>();

        // Assert - Verify that both calls return the exact same cached instance
        Assert.NotNull(handler1);
        Assert.Same(handler1, handler2);
    }

    [Fact]
    public void CreateQueryHandler_ShouldCacheAndReturnSameInstance()
    {
        // Arrange - Create factory with configured options
        using var factory = CreateFactory();

        // Act - Request QueryHandler twice for the same query and projection pair
        var handler1 = factory.CreateQueryHandler<GetActiveCourses, ActiveCourses>();
        var handler2 = factory.CreateQueryHandler<GetActiveCourses, ActiveCourses>();

        // Assert - Verify that both calls return the exact same cached instance
        Assert.NotNull(handler1);
        Assert.Same(handler1, handler2);
    }

    [Fact]
    public async Task Dispose_ShouldDisposeAllCachedHandlersAndManagers()
    {
        // Arrange - Create factory and instantiate handlers
        var factory = CreateFactory();

        var commandHandler = factory.CreateCommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>();
        var queryHandler = factory.CreateQueryHandler<GetActiveCourses, ActiveCourses>();

        // Act - Dispose the factory
        factory.Dispose();

        // Assert - Verify that invoking operations on cached handlers throws ObjectDisposedException
        var command = new SubscribeStudentToCourse(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            commandHandler.ExecuteAsync(command, CancellationToken.None));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            queryHandler.QueryAsync(new GetActiveCourses(), CancellationToken.None));
    }

    [Fact]
    public void CreateCommandHandler_WhenDeciderNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange - Setup factory with empty options (missing DeciderProxy mapping)
        var options = Options.Create(new FilinOptions());
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));

        using var factory = new HandlersFactory(
            new StubEventStore(),
            new StubAppendEventsBlock(),
            options,
            cache,
            saveProjectionBlock);

        // Act & Assert - Expect InvalidOperationException when decider type is missing in options
        Assert.Throws<InvalidOperationException>(factory.CreateCommandHandler<SubscribeStudentToCourse, StudentCourseSubscriptionState>);
    }

    [Fact]
    public void CreateQueryHandler_WhenProjectorNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange - Setup factory with empty options (missing ProjectorProxy mapping)
        var options = Options.Create(new FilinOptions());
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));

        using var factory = new HandlersFactory(
            new StubEventStore(),
            new StubAppendEventsBlock(),
            options,
            cache,
            saveProjectionBlock);

        // Act & Assert - Expect InvalidOperationException when projector type is missing in options
        Assert.Throws<InvalidOperationException>(factory.CreateQueryHandler<GetActiveCourses, ActiveCourses>);
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldBeIdempotent()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert - Calling Dispose multiple times must not throw exceptions
        factory.Dispose();
        factory.Dispose();
    }

    // Helper method to setup HandlersFactory with pre-configured FilinOptions and pure stubs
    static HandlersFactory CreateFactory()
    {
        var filinOptions = new FilinOptions();

        // Register ProjectorProxy mappings for State types
        filinOptions.ProjectorProxyTypes[typeof(ActiveCourses)] =
            typeof(ProjectorProxy<ActiveCoursesProjector, ActiveCourses>);
        filinOptions.ProjectorProxyTypes[typeof(StudentCourseSubscriptionState)] =
            typeof(ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>);

        // Register DeciderProxy mapping for Command and State tuple
        filinOptions.DeciderProxyTypes[(typeof(SubscribeStudentToCourse), typeof(StudentCourseSubscriptionState))] =
            typeof(DeciderProxy<SubscribeStudentToCourseDecider, SubscribeStudentToCourse, StudentCourseSubscriptionState>);

        var options = Options.Create(filinOptions);
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        return new HandlersFactory(
            new StubEventStore(),
            new StubAppendEventsBlock(),
            options,
            cache,
            saveProjectionBlock);
    }
}
