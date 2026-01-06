using Edu.Exceptions;
using Edu.ReadModels;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Projectors;

[ProjectorId("SubscribeStudentToCourseProjector")]
[QueryEventTypes(1, "CourseCreated", "CourseCapacityChanged", "StudentSubscribed", "StudentUnsubscribed")]
[QueryEventTypes(2, "StudentRegistered", "StudentSubscribed", "StudentUnsubscribed")]
[QueryTagParameters(1, "course")]
[QueryTagParameters(2, "student")]
sealed partial class SubscribeStudentToCourseProjector : IProjector<StudentCourseSubscriptionState>
{
    public static StudentCourseSubscriptionState Project(DcbEventSequenced domainEvent) =>
        domainEvent.Event.Data switch
        {
            V1.CourseCreated evt => Create(evt),
            V1.StudentRegistered evt => Create(evt),
            var data => throw UnexpectedEventTypeException(data)
        };

    public static StudentCourseSubscriptionState Project(StudentCourseSubscriptionState state, DcbEventSequenced domainEvent) =>
        domainEvent.Event.Data switch
        {
            V1.CourseCreated evt => Update(state, evt),
            V1.StudentRegistered evt => Update(state, evt),
            V1.CourseCapacityChanged evt => Update(state, evt),
            V1.StudentSubscribed evt => Update(state, evt),
            V1.StudentUnsubscribed evt => Update(state, evt),
            var data => throw UnexpectedEventTypeException(data)
        };

    static StudentCourseSubscriptionState Create(V1.CourseCreated evt) => new() { StudentId = default, CourseId = evt.CourseId };

    static StudentCourseSubscriptionState Create(V1.StudentRegistered evt) => new() { StudentId = evt.StudentId, CourseId = default };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.CourseCreated evt) => state with { CourseId = evt.CourseId };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.StudentRegistered evt) => state with { StudentId = evt.StudentId };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.CourseCapacityChanged evt) => state with { CourseCapacity = evt.Capacity };

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.StudentUnsubscribed evt)
    {
        var totalCount = state.CourseSubscriptionCount - 1;

        if (state.StudentId == evt.StudentId)
        {
            return state with
            {
                SubscribedCourseIds = state.SubscribedCourseIds.Remove(evt.CourseId),
                CourseSubscriptionCount = totalCount
            };
        }

        return state with { CourseSubscriptionCount = totalCount };
    }

    static StudentCourseSubscriptionState Update(StudentCourseSubscriptionState state, V1.StudentSubscribed evt)
    {
        var totalCount = state.CourseSubscriptionCount + 1;

        if (state.StudentId == evt.StudentId)
        {
            return state with
            {
                SubscribedCourseIds = state.SubscribedCourseIds.Add(evt.CourseId),
                CourseSubscriptionCount = totalCount
            };
        }

        return state with { CourseSubscriptionCount = totalCount };
    }

    static UnexpectedEventTypeException UnexpectedEventTypeException(object? evt) =>
        new UnexpectedEventTypeException(evt?.GetType())
            .EnrichWithProjector(typeof(StudentCourseSubscriptionState));
}
