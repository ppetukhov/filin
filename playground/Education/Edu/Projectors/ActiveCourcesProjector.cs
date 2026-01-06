using System.Collections.Immutable;

using Edu.Exceptions;
using Edu.ReadModels;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Projectors;

/// <inheritdoc cref="IProjector{ActiveCourses}"/>
[ProjectorId("ActiveCoursesProjector")]
[QueryEventTypes("CourseCreated", "CourseRenamed")]
sealed partial class ActiveCoursesProjector : IProjector<ActiveCourses>
{
    /// <inheritdoc/>
    public static ActiveCourses Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Data switch
        {
            V1.CourseCreated evt => Add(evt.CourseId, evt.Name),
            V1.CourseRenamed evt => Add(evt.CourseId, evt.NewName),
            var evt => throw UnexpectedEventTypeException(evt)
        };

    /// <inheritdoc/>
    public static ActiveCourses Project(ActiveCourses state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Data switch
        {
            V1.CourseCreated evt => Upsert(state, evt.CourseId, evt.Name),
            V1.CourseRenamed evt => Upsert(state, evt.CourseId, evt.NewName),
            var data => throw UnexpectedEventTypeException(data)
        };

    static ActiveCourses Add(Guid courseId, string name) => new() { Courses = ImmutableDictionary<Guid, string>.Empty.Add(courseId, name) };

    static ActiveCourses Upsert(ActiveCourses state, Guid courseId, string courseName) => state with { Courses = state.Courses.SetItem(courseId, courseName) };

    static UnexpectedEventTypeException UnexpectedEventTypeException(object? evt) =>
        new UnexpectedEventTypeException(evt?.GetType())
            .EnrichWithProjector(typeof(ActiveCourses));
}
