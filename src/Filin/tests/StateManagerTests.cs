using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Logging.Abstractions;

namespace Filin.Tests;

public class StateManagerTests
{
    [Fact]
    public void ApplyEvent_WithNullState_ShouldProjectInitialStateAndSetCreatedStatus()
    {
        // Arrange - Instantiate StateManager dependencies
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        StudentCourseSubscriptionState? state = null;
        var metadata = new StateMetadata(new EventPosition(0), DateTimeOffset.MinValue, StateStatus.Unchanged);

        var courseId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var domainEvent = CreateSequencedEvent(new DomainEvents.V1.CourseCreated(courseId, "C# Event Sourcing"), position: new EventPosition(10), timestamp: timestamp);

        // Act - Apply the event when initial state is null
        stateManager.ApplyEvent(ref state, ref metadata, ref domainEvent);

        // Assert - Verify state is created and metadata status is updated to Created
        Assert.NotNull(state);
        Assert.Equal(courseId, state.CourseId);
        Assert.Equal(StateStatus.Created, metadata.Status);
        Assert.Equal(new EventPosition(10), metadata.Position);
        Assert.Equal(timestamp, metadata.Timestamp);
    }

    [Fact]
    public void ApplyEvent_WithExistingState_ShouldUpdateStateAndSetUpdatedStatus()
    {
        // Arrange - Setup initial state and StateManager
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var state = new StudentCourseSubscriptionState
        {
            CourseId = courseId,
            StudentId = studentId
        };
        var metadata = new StateMetadata(new EventPosition(1), DateTimeOffset.UtcNow.AddMinutes(-5), StateStatus.Unchanged);

        var timestamp = DateTimeOffset.UtcNow;
        var domainEvent = CreateSequencedEvent(new DomainEvents.V1.StudentSubscribed(courseId, studentId), position: new EventPosition(2), timestamp: timestamp);

        // Act - Apply event to fold existing state
        stateManager.ApplyEvent(ref state, ref metadata, ref domainEvent);

        // Assert - Verify state was folded and metadata status changed to Updated
        Assert.NotNull(state);
        Assert.Contains(courseId, state.SubscribedCourseIds);
        Assert.Equal(StateStatus.Updated, metadata.Status);
        Assert.Equal(new EventPosition(2), metadata.Position);
        Assert.Equal(timestamp, metadata.Timestamp);
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange & Act - Instantiate StateManager
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        using var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        // Assert - Ensure state manager correctly assigns internal proxies and Dataflow blocks
        Assert.NotNull(stateManager.Projector);
        Assert.NotNull(stateManager.LoadStateBlock);
        Assert.NotNull(stateManager.SaveStateBlock);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotentAndNotThrow()
    {
        // Arrange
        var projector = new ProjectorProxy<SubscribeStudentToCourseProjector, StudentCourseSubscriptionState>();
        var cache = new LRUCache<ProjectionId, Task<Projection?>>(capacity: 10, ttl: TimeSpan.FromMinutes(1));
        using var saveProjectionBlock = new SaveProjectionBlock(NullLoggerFactory.Instance, projectionStore: null, timeProvider: null);

        var stateManager = new StateManager<StudentCourseSubscriptionState>(
            eventStore: null!,
            appendEventsBlock: new StubAppendEventsBlock(),
            projector: projector,
            cache: cache,
            saveProjectionBlock: saveProjectionBlock,
            projectionStore: null
        );

        // Act & Assert - Multiple Dispose calls should execute cleanly without exceptions
        stateManager.Dispose();
        stateManager.Dispose();
    }

    // Helper method to create sequenced DCB events
    static DcbEventSequenced CreateSequencedEvent(object payload, EventPosition position, DateTimeOffset timestamp) =>
        new(
            Position: position,
            Timestamp: timestamp,
            Event: new DcbEvent { Payload = payload, Tags = default, EventType = "StudentSubscribed" }
        );
}
