## About

<!-- A description of the package and where one can find more documentation -->

`Filin.Abstractions` provides core interfaces and abstractions for the **Filin Event Sourcing framework** with native support for **Dynamic Consistency Boundaries (DCB)**. These contracts are implemented by the core `Filin` library and its ecosystem packages.

This package includes an incremental source code generator that automatically inspects attributes on your **Events, Deciders, and Projectors** to emit optimized boilerplate for:
* **EventStore** - constructing context-sensitive queries based on event types and event tags annotations. Following the DCB specification, it facilitates optimistic concurrency checks tailored strictly to the exact history subset needed for business decisions.
* **ProjectionStore** - managing read-model states and event-driven projections.

## Key Features

<!-- The key features of this package -->

* Define main event sourcing with DCB abstraction interfaces like `IMediator`, `ICommand<TState>`, `IQuery<TProjection>`, `IProjector<TState>`, `IDecider<TCommand, TState>`, `IEventStore`, `IProjectionStore` etc.
* Define annotation attributes `[EventType]`, `[EventTag]`, `[QueryEventTypes]`, `[QueryTagParameters]`, `[QueryTags]` `[ProjectorId]`, `[Decider]`.
* Define DCB primitives like `DcbQuery`, `EventType`, `Tag`, `EventPosition`, etc.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

#### Domain events annotated by attributes

```csharp
using Filin;

static partial class DomainEvents
{
    internal static partial class V1
    {
        [EventType("CourseCreated")]
        public sealed record CourseCreated([EventTag("course")] Guid CourseId, string Name);

        [EventType("StudentRegistered")]
        public sealed record StudentRegistered([EventTag("student")] public Guid StudentId, string Name);

        [EventType("StudentSubscribed")]
        public sealed record StudentSubscribed([EventTag("course")] Guid CourseId, [EventTag("student")] Guid StudentId);
    }
}
```

#### Command annotated by tag attributes

```csharp
using Filin;

/// <summary>
/// Represents a command to subscribe a student to a specific course.
/// </summary>
/// <param name="CourseId">The unique identifier of the target course.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
public sealed partial record SubscribeStudentToCourse([EventTag("course")] Guid CourseId, [EventTag("student")] Guid StudentId) : ICommand<StudentCourseSubscriptionState>;

/// <summary>
/// Holds the consolidated read-state required to validate a student's subscription to a specific course.
/// </summary>
public sealed record StudentCourseSubscriptionState
{
    /// <summary>
    /// Gets the unique identifier of the student.
    /// </summary>
    [JsonPropertyName("student_id")]
    public required Guid StudentId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the target course.
    /// </summary>
    [JsonPropertyName("course_id")]
    public required Guid CourseId { get; init; }

    /// <summary>
    /// Gets the set of course IDs the student is currently subscribed to.
    /// </summary>
    [JsonPropertyName("subscribed_course_ids")]
    public ImmutableHashSet<Guid> SubscribedCourseIds { get; init; } = [];
}
```

#### Projector annotated by attributes

```csharp
using Filin;
using V1 = DomainEvents.V1;

[ProjectorId("SubscribeStudentToCourseProjector")]
[QueryEventTypes(1, "CourseCreated")]
[QueryEventTypes(2, "StudentRegistered", "StudentSubscribed")]
[QueryTagParameters(1, "course")]
[QueryTagParameters(2, "student")]
sealed partial class SubscribeStudentToCourseProjector : IProjector<StudentCourseSubscriptionState>
{
    // Evaluated when building state from the first relevant event found in the log
    public static StudentCourseSubscriptionState Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => Create(evt),
            V1.StudentRegistered evt => Create(evt),
            var evt => throw UnexpectedEventTypeException(evt)
        };

    // Evaluated for all subsequent relevant events to continually hydrate the projection
    public static StudentCourseSubscriptionState Project(StudentCourseSubscriptionState state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => Update(state, evt),
            V1.StudentRegistered evt => Update(state, evt),
            V1.StudentSubscribed evt => Update(state, evt),
            var evt => throw UnexpectedEventTypeException(evt)
        };

    static StudentCourseSubscriptionState Create(V1.CourseCreated evt) => new() { StudentId = default, CourseId = evt.CourseId };

    static StudentCourseSubscriptionState Create(V1.StudentRegistered evt) => new() { StudentId = evt.StudentId, CourseId = default };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.CourseCreated evt) => state with { CourseId = evt.CourseId };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.StudentRegistered evt) => state with { StudentId = evt.StudentId };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.StudentSubscribed evt) => state with
    {
        SubscribedCourseIds = state.SubscribedCourseIds.Add(evt.CourseId)
    };

    static UnexpectedEventTypeException UnexpectedEventTypeException(object? evt) => new UnexpectedEventTypeException(evt?.GetType());
}
```

#### Decider annotated by attributes

```csharp
using Filin;

[Decider]
sealed class SubscribeStudentToCourseDecider : IDecider<SubscribeStudentToCourse, StudentCourseSubscriptionState>
{
    // Fallback: Invoked when the system finds zero historical events for the requested context tags
    public static DecideResult Decide(SubscribeStudentToCourse command) => DomainErrors.Course.NotFound(command.CourseId);

    // Primary Execution: Invoked once the DCB store has projected the log history into the local state
    public static DecideResult Decide(SubscribeStudentToCourse command, StudentCourseSubscriptionState state)
    {
        if (state.StudentId == default)
        {
            return DomainErrors.Student.NotFound(command.StudentId);
        }

        if (state.CourseId == default)
        {
            return DomainErrors.Course.NotFound(command.CourseId);
        }

        if (state.SubscribedCourseIds.Contains(command.CourseId))
        {
            return DomainErrors.Student.AlreadySubscribed(command.StudentId, command.CourseId);
        }

        // Return the successfully produced domain event, inheriting tags directly from the command context
        return new V1.StudentSubscribed(CourseId: command.CourseId, StudentId: command.StudentId)
                        .ToDcbEvent(command.GetTags());
    }
}

/// <summary>
/// Centralized factory for all domain and business errors.
/// </summary>
static class DomainErrors
{
    internal static class Student
    {
        internal static DecideResult NotFound(Guid studentId) => ("STUDENT_NOT_FOUND", $"Student with ID '{studentId}' was not found.");

        internal static DecideResult AlreadySubscribed(Guid studentId, Guid courseId) => ("STUDENT_ALREADY_SUBSCRIBED", $"Student with ID '{studentId}' is already subscribed to course with ID '{courseId}'.");
    }

    internal static class Course
    {
        internal static DecideResult NotFound(Guid courseId) => ("COURSE_NOT_FOUND", $"Course with ID '{courseId}' was not found.");
    }
}
```

#### Execution command via Mediator

```csharp
using Filin;

// Endpoint for creating a student course subscription
app.MapPost("/api/courses/{courseId:guid}/subscribes", async (
    Guid courseId, 
    [FromBody] SubscribeRequest request, 
    IMediator mediator, 
    ILogger<Program> logger, 
    CancellationToken cancellationToken) =>
{
    var command = new SubscribeStudentToCourse(courseId, request.StudentId);

    var res = await mediator.ExecuteAsync<SubscribeStudentToCourse, StudentCourseSubscriptionState>(command, cancellationToken);

    if (res.IsSuccess)
    {
        logger.LogInformation("Student {StudentId} successfully subscribed to course {CourseId}", request.StudentId, courseId);
        
        var locationUri = $"/api/courses/{courseId}/subscribes/{request.StudentId}";
        return Results.Created(locationUri, res.State); 
    }
    else
    {
        logger.LogWarning("Business rule violation when subscribing student {StudentId} to course {CourseId}: {ErrorCode} {ErrorMessage}", request.StudentId, courseId, res.Error.Code, res.Error.Message);
        
        // Returns an RFC 7807 compliant 422 Problem Details response
        return Results.Problem(
            detail: res.Error.Message, // The actual domain message (e.g., "Course is full")
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Business Rule Violation",
            type: $"https://example.com/errors/{res.Error.Code}" // Optional URI reference to error type
        );
    }
})
.WithName("SubscribeStudent");

public record SubscribeRequest(Guid StudentId);
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Filin.IMediator`
* `Filin.ICommand<TState>`
* `Filin.IQuery<TProjection>`
* `Filin.IProjector<TState>`
* `Filin.IDecider<TCommand, TState>`
* `Filin.Storage.IEventStore`
* `Filin.Storage.IProjectionStore`
* `Filin.EventTypeAttribute`
* `Filin.EventTagAttribute`
* `Filin.QueryEventTypesAttribute`
* `Filin.QueryTagParametersAttribute`
* `Filin.QueryTagsAttribute`
* `Filin.ProjectorIdAttribute`
* `Filin.DeciderAttribute`
* `Filin.DcbQuery`
* `Filin.EventType`
* `Filin.Tag`
* `Filin.EventPosition`

## Additional Documentation

<!-- Links to documentation -->

* [Filin framework](https://github.com/ppetukhov/filin)
* [DCB Specification](https://dcb.events/specification/) - Dynamic Consistency Boundaries pattern

## Related Packages

<!-- The related packages associated with this package -->

| Package | Description |
|---------|-------------|
| [Filin](https://www.nuget.org/packages/Filin) | Core implementation of the Filin `IMediator`. |
| [Filin.EventStore.AdoNet](https://www.nuget.org/packages/Filin.EventStore.AdoNet) | ADO.NET-based implementation of the Filin `IEventStore`. |
| [Filin.ProjectionStore.AdoNet](https://www.nuget.org/packages/Filin.ProjectionStore.AdoNet) | ADO.NET-based implementation of the Filin `IProjectionStore`. |

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Filin.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/ppetukhov/filin).