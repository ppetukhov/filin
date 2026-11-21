using System.Collections.Immutable;

namespace Filin.Tests;

public class ActiveCoursesProjectorTests
{
    [Fact]
    public void Project_InitialCourseCreatedEvent_ShouldInitializeStateWithCourse()
    {
        // Arrange - Create an initial domain event for course creation
        var courseId = Guid.NewGuid();
        const string courseName = "Architecture & Event Sourcing";
        var domainEvent = CreateSequencedEvent(new DomainEvents.V1.CourseCreated(courseId, courseName));

        // Act - Invoke initial state projection
        var state = ActiveCoursesProjector.Project(domainEvent);

        // Assert
        Assert.True(state.Courses.ContainsKey(courseId));
        Assert.Equal(courseName, state.Courses[courseId]);
    }

    [Fact]
    public void Project_SubsequentCourseCreatedEvent_ShouldAddCourseToExistingState()
    {
        // Arrange - Set up initial state with one course
        var existingCourseId = Guid.NewGuid();
        var initialState = new ActiveCourses
        {
            Courses = ImmutableDictionary<Guid, string>.Empty.Add(existingCourseId, "C# Basics")
        };

        var newCourseId = Guid.NewGuid();
        const string newCourseName = "Advanced Event Sourcing";
        var domainEvent = CreateSequencedEvent(new DomainEvents.V1.CourseCreated(newCourseId, newCourseName));

        // Act - Fold existing state with the new course event
        var updatedState = ActiveCoursesProjector.Project(initialState, domainEvent);

        // Assert
        Assert.Equal(2, updatedState.Courses.Count);
        Assert.Equal(newCourseName, updatedState.Courses[newCourseId]);
    }

    // Helper method to create sequenced DCB events
    static DcbEventSequenced CreateSequencedEvent(object payload) =>
        new(
            Position: new EventPosition(1),
            Timestamp: DateTimeOffset.UtcNow,
            Event: new DcbEvent { Payload = payload, Tags = default, EventType = "CourseCreated" }
        );
}
