using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Filin.Tests;

// --- Domain Events ---
static partial class DomainEvents
{
    internal static partial class V1
    {
        [EventType("CourseCreated")]
        public sealed record CourseCreated([EventTag("course")] Guid CourseId, string Name);

        [EventType("StudentRegistered")]
        public sealed record StudentRegistered([EventTag("student")] Guid StudentId, string Name);

        [EventType("StudentSubscribed")]
        public sealed record StudentSubscribed([EventTag("course")] Guid CourseId, [EventTag("student")] Guid StudentId);
    }
}

// --- Commands and State ---
public sealed partial record SubscribeStudentToCourse(
    [EventTag("course")] Guid CourseId,
    [EventTag("student")] Guid StudentId
) : ICommand<StudentCourseSubscriptionState>;

public sealed record StudentCourseSubscriptionState
{
    [JsonPropertyName("student_id")]
    public required Guid StudentId { get; init; }

    [JsonPropertyName("course_id")]
    public required Guid CourseId { get; init; }

    [JsonPropertyName("subscribed_course_ids")]
    public ImmutableHashSet<Guid> SubscribedCourseIds { get; init; } = [];
}

// --- Decider ---
[Decider]
sealed class SubscribeStudentToCourseDecider : IDecider<SubscribeStudentToCourse, StudentCourseSubscriptionState>
{
    // Fallback decision when no state history exists in event store
    public static DecideResult Decide(SubscribeStudentToCourse command) =>
        ("COURSE_NOT_FOUND", $"Course '{command.CourseId}' not found.");

    // Decision logic execution based on loaded state
    public static DecideResult Decide(SubscribeStudentToCourse command, StudentCourseSubscriptionState state)
    {
        if (state.StudentId == Guid.Empty)
        {
            return ("STUDENT_NOT_FOUND", "Student not found.");
        }

        if (state.CourseId == Guid.Empty)
        {
            return ("COURSE_NOT_FOUND", "Course not found.");
        }

        return state.SubscribedCourseIds.Contains(command.CourseId)
            ? (DecideResult)("STUDENT_ALREADY_SUBSCRIBED", "Already subscribed.")
            : (DecideResult)new DomainEvents.V1.StudentSubscribed(command.CourseId, command.StudentId)
            .ToDcbEvent(command.GetTags());
    }
}

// --- Query and Projection ---
public sealed record GetActiveCourses : IQuery<ActiveCourses>
{
    public Tags GetTags() => [];
}

public sealed record ActiveCourses
{
    [JsonPropertyName("courses")]
    public required ImmutableDictionary<Guid, string> Courses { get; init; }
}

[ProjectorId("SubscribeStudentToCourseProjector")]
[QueryEventTypes(1, "CourseCreated")]
[QueryEventTypes(2, "StudentRegistered", "StudentSubscribed")]
[QueryTagParameters(1, "course")]
[QueryTagParameters(2, "student")]
sealed partial class SubscribeStudentToCourseProjector : IProjector<StudentCourseSubscriptionState>
{
    // Initial state projection from first event
    public static StudentCourseSubscriptionState Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            DomainEvents.V1.CourseCreated evt => new StudentCourseSubscriptionState { StudentId = Guid.Empty, CourseId = evt.CourseId },
            DomainEvents.V1.StudentRegistered evt => new StudentCourseSubscriptionState { StudentId = evt.StudentId, CourseId = Guid.Empty },
            _ => throw new InvalidOperationException()
        };

    // State folding for subsequent domain events
    public static StudentCourseSubscriptionState Project(StudentCourseSubscriptionState state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            DomainEvents.V1.CourseCreated evt => state with { CourseId = evt.CourseId },
            DomainEvents.V1.StudentRegistered evt => state with { StudentId = evt.StudentId },
            DomainEvents.V1.StudentSubscribed evt => state with { SubscribedCourseIds = state.SubscribedCourseIds.Add(evt.CourseId) },
            _ => throw new InvalidOperationException()
        };
}

[ProjectorId("ActiveCoursesProjector")]
[QueryEventTypes(1, "CourseCreated")]
public sealed partial class ActiveCoursesProjector : IProjector<ActiveCourses>
{
    // Initial state projection from first event
    public static ActiveCourses Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            DomainEvents.V1.CourseCreated evt => new ActiveCourses
            {
                Courses = ImmutableDictionary<Guid, string>.Empty.Add(evt.CourseId, evt.Name)
            },
            _ => throw new InvalidOperationException()
        };

    // State folding for subsequent domain events
    public static ActiveCourses Project(ActiveCourses state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            DomainEvents.V1.CourseCreated evt => state with
            {
                Courses = state.Courses.SetItem(evt.CourseId, evt.Name)
            },
            _ => throw new InvalidOperationException()
        };
}
