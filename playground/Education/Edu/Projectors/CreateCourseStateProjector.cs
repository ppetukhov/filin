using Edu.Exceptions;
using Edu.ReadModels;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Projectors;

[ProjectorId("CreateCourse")]
[QueryEventTypes(1, "CourseCreated", "CourseRenamed")]
[QueryTagParameters(1, "course")]
[QueryEventTypes(2, "CourseCreated", "CourseRenamed")]
[QueryTagParameters(2, "course_name")]
sealed partial class CreateCourseStateProjector : IProjector<CreateCourseState>
{
    public static CreateCourseState Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => new CreateCourseState { CourseId = evt.CourseId, CourseName = evt.Name },
            V1.CourseRenamed evt => new CreateCourseState { ExistNames = [evt.NewName] },
            var evt => throw UnexpectedEventTypeException(evt)
        };

    public static CreateCourseState Project(CreateCourseState state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Payload switch
        {
            V1.CourseCreated evt => Apply(state, evt),
            V1.CourseRenamed evt => Apply(state, evt),
            var data => throw UnexpectedEventTypeException(data)
        };


    static CreateCourseState Apply(CreateCourseState state, V1.CourseCreated evt)
    {
        if (state.CourseId is not null)
        {
            throw new InvalidStateException($"Error apply event {typeof(V1.CourseCreated).FullName} with CourseId {evt.CourseId} to {typeof(CreateCourseState)} because it's already initialized with CourseId: {state.CourseId}")
                .EnrichWithProjector(typeof(CreateCourseStateProjector));
        }

        return state with
        {
            CourseId = evt.CourseId,
            CourseName = evt.Name
        };
    }

    static CreateCourseState Apply(CreateCourseState state, V1.CourseRenamed evt)
    {
        if (state.CourseId == evt.CourseId)
        {
            return state with
            {
                CourseName = evt.NewName,
                ExistNames = state.ExistNames?.Remove(evt.OldName)
            };
        }

        return state with
        {
            ExistNames = state.ExistNames is null ? [evt.NewName] : state.ExistNames.Remove(evt.OldName).Add(evt.NewName)
        };
    }

    static UnexpectedEventTypeException UnexpectedEventTypeException(object? evt) =>
        new UnexpectedEventTypeException(evt?.GetType())
            .EnrichWithProjector(typeof(CreateCourseStateProjector));
}
