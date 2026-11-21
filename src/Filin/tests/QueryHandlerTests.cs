using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Logging.Abstractions;

namespace Filin.Tests;

public class QueryHandlerTests
{
    [Fact]
    public async Task QueryAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange - Setup StateManager with required dependencies
        var projector = new ProjectorProxy<ActiveCoursesProjector, ActiveCourses>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<ActiveCourses>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        var queryHandler = new QueryHandler<GetActiveCourses, ActiveCourses>(stateManager);

        // Act - Dispose the QueryHandler instance
        queryHandler.Dispose();

        // Assert - Verify that invoking QueryAsync on a disposed handler throws ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            queryHandler.QueryAsync(new GetActiveCourses(), CancellationToken.None));
    }

    [Fact]
    public async Task QueryAsync_WhenStateIsNull_ShouldReturnNullWithoutSaving()
    {
        // Arrange - Setup projector proxy, cache, and logger
        var projector = new ProjectorProxy<ActiveCoursesProjector, ActiveCourses>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<ActiveCourses>(
            eventStore: new StubEventStore(),
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        using var queryHandler = new QueryHandler<GetActiveCourses, ActiveCourses>(stateManager);

        // Act - Query when state is not present in cache or event store
        var result = await queryHandler.QueryAsync(new GetActiveCourses(), CancellationToken.None);

        // Assert - Verify that the result is null
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_WhenStateIsLoaded_ShouldReturnStateAndPostToSaveBlock()
    {
        // Arrange - Setup projector proxy, cache, and logger
        var projector = new ProjectorProxy<ActiveCoursesProjector, ActiveCourses>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<ActiveCourses>(
            eventStore: new StubEventStore(),
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        // Pre-populate cache with an existing projection state
        var query = new GetActiveCourses();
        var projectionId = projector.GetProjectionId(query.GetTags());
        var loadedState = new ActiveCourses { Courses = [] };
        var projection = new Projection
        {
            Id = projectionId,
            Position = new EventPosition(1),
            Timestamp = DateTimeOffset.UtcNow,
            Payload = loadedState
        };
        cache.Set(projectionId, Task.FromResult<Projection?>(projection));

        using var queryHandler = new QueryHandler<GetActiveCourses, ActiveCourses>(stateManager);

        // Act - Execute the query asynchronously
        var result = await queryHandler.QueryAsync(query, CancellationToken.None);

        // Assert - Verify that loaded state is returned correctly
        Assert.NotNull(result);
        Assert.Equal(loadedState, result);
    }

    [Fact]
    public async Task QueryAsync_WhenLoadStateBlockCompleted_ShouldThrowMediatorException()
    {
        // Arrange - Use ActiveCoursesProjector and ActiveCourses (does not require "course:" tag)
        var projector = new ProjectorProxy<ActiveCoursesProjector, ActiveCourses>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<ActiveCourses>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        using var queryHandler = new QueryHandler<GetActiveCourses, ActiveCourses>(stateManager);

        // Complete the block to force LoadStateBlock.Post() to return false
        stateManager.LoadStateBlock.Complete();

        // Act & Assert - Expect MediatorException ("System too busy") when dataflow block is completed
        var ex = await Assert.ThrowsAsync<MediatorException>(() =>
            queryHandler.QueryAsync(new GetActiveCourses(), CancellationToken.None));

        Assert.Equal("System too busy", ex.Message);
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldBeIdempotent()
    {
        // Arrange
        var projector = new ProjectorProxy<ActiveCoursesProjector, ActiveCourses>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
#pragma warning disable CA2000 // Dispose objects before losing scope
        var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);
#pragma warning restore CA2000 // Dispose objects before losing scope

        using var stateManager = new StateManager<ActiveCourses>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        var queryHandler = new QueryHandler<GetActiveCourses, ActiveCourses>(stateManager);

        // Act & Assert - Calling Dispose multiple times should not throw
        queryHandler.Dispose();
        queryHandler.Dispose();
    }
}
