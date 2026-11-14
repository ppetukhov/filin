## About

<!-- A description of the package and where one can find more documentation -->

`Filin` provides a Mediator implementation for the **Filin Event Sourcing framework**.

## Key Features

<!-- The key features of this package -->

* **Unified Execution**: Implements `Filin.IMediator` to coordinate execution across the framework, exposing a single interface to process both operational write **Commands** and read-optimized **Queries**.
* **JSON Serialization**: Provides the default JSON storage serializer implementation for `Filin.Storage.IStorageSerializer`.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

#### Configure Filin

Register **Filin** services in your dependency injection container and configure the database connections. 

Add the configuration logic to your `Program.cs`:

```csharp
using Filin.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFilin()
    // Auto-discover events, commands, queries, projectors and projections
    .AddTypeManifestAssembly(typeof(DomainEvents.V1.CourseCreated).Assembly, typeof(SubscribeStudentToCourse).Assembly);

// Register ADO.NET-based implementation of IEventStore` and IAppendEventsBlock (required Filin.EventStore.AdoNet)
builder.Services.AddEventStoreAdoNet();

// Register ADO.NET-based implementation of `Filin.Storage.IProjectionStore` (required Filin.ProjectionStore.AdoNet)
builder.Services.AddProjectionStoreAdoNet()

// Registration of other services skipped...

var app = builder.Build();

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

app.MapGet("/api/courses", async (IMediator mediator, CancellationToken cancellationToken) =>
{
    var query = new GetActiveCourses();

    // Fetches cache state, catches up from log via ActiveCoursesProjector, saves cache, and returns view
    var result = await mediator.QueryAsync<GetActiveCourses, ActiveCourses>(query, cancellationToken);

    return Results.Ok(result ?? new ActiveCourses { Courses = System.Collections.Immutable.ImmutableDictionary<Guid, string>.Empty });

})
.WithName("GetActiveCourses")

app.Run();

public record SubscribeRequest(Guid StudentId);
```

*Note. Ensure that implementations of `Filin.Storage.IEventStore`, `Filin.Storage.DataFlow.IAppendEventsBlock`, and `Filin.Storage.IProjectionStore` are registered in your DI container.
You can use the [Filin.EventStore.AdoNet](https://www.nuget.org/packages/Filin.EventStore.AdoNet) and [Filin.ProjectionStore.AdoNet](https://www.nuget.org/packages/Filin.ProjectionStore.AdoNet) libraries.*

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

#### Define the Query and Target View State

```csharp
using Filin;

/// <summary>
/// Represents a query request to retrieve all active courses.
/// </summary>
public sealed record GetActiveCourses : IQuery<ActiveCourses>
{
    /// <inheritdoc/>
    public Tags GetTags() => [];
}

/// <summary>
/// Represents the domain read-state containing all active courses.
/// </summary>
public sealed record ActiveCourses
{
    /// <summary>
    /// Gets the collection of active courses mapped by their unique identifiers and names.
    /// </summary>
    [JsonPropertyName("courses")]
    public required ImmutableDictionary<Guid, string> Courses { get; init; }
}
```

#### Define the Dedicated Query Projector

```csharp
using Filin;

[ProjectorId("ActiveCoursesProjector")]
[QueryEventTypes("CourseCreated")]
sealed partial class ActiveCoursesProjector : IProjector<ActiveCourses>
{
    /// <inheritdoc/>
    public static ActiveCourses Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => Add(evt.CourseId, evt.Name),
            var evt => throw UnexpectedEventTypeException(evt)
        };

    /// <inheritdoc/>
    public static ActiveCourses Project(ActiveCourses state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => Upsert(state, evt.CourseId, evt.Name),
            var evt => throw UnexpectedEventTypeException(evt)
        };

    static ActiveCourses Add(Guid courseId, string name) => new() { Courses = ImmutableDictionary<Guid, string>.Empty.Add(courseId, name) };

    static ActiveCourses Upsert(ActiveCourses state, Guid courseId, string courseName) => state with { Courses = state.Courses.SetItem(courseId, courseName) };

    static UnexpectedEventTypeException UnexpectedEventTypeException(object? evt) =>
        new UnexpectedEventTypeException(evt?.GetType());
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Filin.Configuration.JsonStorageSerializerOptions`
* `Filin.DependencyInjection.FilinServiceCollectionExtensions`

## Additional Documentation

<!-- Links to documentation -->

* [Filin framework](https://github.com/ppetukhov/filin)
* [DCB Specification](https://dcb.events/specification/) - Dynamic Consistency Boundaries pattern

## Related Packages

<!-- The related packages associated with this package -->

| Package | Description |
|---------|-------------|
| [Filin.Abstractions](https://www.nuget.org/packages/Filin.Abstractions) | Contains core Filin primitives, annotation attributes, and interfaces. |
| [Filin.EventStore.AdoNet](https://www.nuget.org/packages/Filin.EventStore.AdoNet) | ADO.NET-based implementation of the Filin `IEventStore`. |
| [Filin.ProjectionStore.AdoNet](https://www.nuget.org/packages/Filin.ProjectionStore.AdoNet) | ADO.NET-based implementation of the Filin `IProjectionStore`. |

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Filin.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/ppetukhov/filin).