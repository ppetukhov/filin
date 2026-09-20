using System.Diagnostics;

using Edu.ReadModels;
using Edu.UseCases;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Deciders;

/// <inheritdoc cref="IDecider{RenameCourse, CreateCourseState}"/>
[Decider]
sealed class RenameCourseDecider : IDecider<RenameCourse, CreateCourseState>
{
    /// <inheritdoc/>
    public static DecideResult Decide(RenameCourse command) => DomainErrors.Course.NotFound(command.CourseId);

    ///// <inheritdoc/>
    //public static DecideResult Decide(RenameCourse command, ActiveCourses state)
    //{
    //    if (!state.Courses.TryGetValue(command.CourseId, out var currentCourseName))
    //    {
    //        return DomainErrors.Course.NotFound(command.CourseId);
    //    }

    //    if (StringComparer.Ordinal.Equals(currentCourseName, command.NewName))
    //    {
    //        return DomainErrors.Course.NameAlreadyCurrent(currentCourseName);
    //    }

    //    if (!StringComparer.Ordinal.Equals(currentCourseName, command.OldName))
    //    {
    //        return DomainErrors.Course.NameMismatch(command.CourseId, command.OldName);
    //    }

    //    bool Predicate(KeyValuePair<Guid, string> kv) => kv.Key != command.CourseId && StringComparer.OrdinalIgnoreCase.Equals(kv.Value, command.NewName);

    //    return state.Courses.FirstOrDefault(Predicate, defaultValue: new KeyValuePair<Guid, string>(default, string.Empty)) is { Value.Length: > 0 } existingCourse
    //        ? DomainErrors.Course.DuplicateName(existingCourse.Value, existingCourse.Key)
    //        : CourseRenamed(command, currentCourseName);
    //}

    public static DecideResult Decide(RenameCourse command, CreateCourseState state)
    {
        if (state.CourseId is null)
        {
            return DomainErrors.Course.NotFound(command.CourseId);
        }

        Debug.Assert(state.CourseId == command.CourseId);

        if (StringComparer.Ordinal.Equals(state.CourseName, command.NewName))
        {
            return DomainErrors.Course.NameAlreadyCurrent(state.CourseName);
        }

        if (!StringComparer.Ordinal.Equals(state.CourseName, command.OldName))
        {
            return DomainErrors.Course.NameMismatch(command.CourseId, command.OldName);
        }

        if (state.ExistNames is not null && state.ExistNames.Contains(command.NewName))
        {
            DomainErrors.Course.DuplicateName(command.NewName);
        }

        return CourseRenamed(command, state.CourseName);
    }

    static DecideResult CourseRenamed(RenameCourse command, string currentName) => new V1.CourseRenamed(command.CourseId, command.NewName, OldName: currentName).ToDcbEvent(command.GetTags());
}
