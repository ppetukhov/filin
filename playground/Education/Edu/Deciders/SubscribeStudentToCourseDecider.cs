using Edu.ReadModels;
using Edu.UseCases;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Deciders;

[Decider]
sealed class SubscribeStudentToCourseDecider : IDecider<SubscribeStudentToCourse, StudentCourseSubscriptionState>
{
    const int MaxStudentSubsriptions = 10; // Maximum number of courses a student can be subscribed to.
    const int DefaultMaxCourseSubscriptions = 25; // Maximum number of students allowed per course.

    public static DecideResult Decide(SubscribeStudentToCourse command) => DomainErrors.Course.NotFound(command.CourseId);

    public static DecideResult Decide(SubscribeStudentToCourse command, StudentCourseSubscriptionState state)
    {
        if (!state.StudentExists)
        {
            return DomainErrors.Student.NotFound(command.StudentId);
        }

        if (!state.CourseExists)
        {
            return DomainErrors.Course.NotFound(command.CourseId);
        }

        if (state.SubscribedCourseIds.Count >= MaxStudentSubsriptions)
        {
            return DomainErrors.Student.SubscriptionLimitReached(command.StudentId, MaxStudentSubsriptions);
        }

        if (state.CourseCapacity is 0 && state.CourseSubscriptionCount >= DefaultMaxCourseSubscriptions)
        {
            return DomainErrors.Course.SubscriptionLimitReached(command.CourseId, DefaultMaxCourseSubscriptions);
        }

        if (state.IsCourseFull)
        {
            return DomainErrors.Course.SubscriptionLimitReached(command.CourseId, state.CourseCapacity);
        }

        if (state.IsAlreadySubscribed)
        {
            return DomainErrors.Student.AlreadySubscribed(command.StudentId, command.CourseId);
        }

        return new V1.StudentSubscribed(CourseId: command.CourseId, StudentId: command.StudentId)
                        .ToDcbEvent(command.GetTags());
    }
}
