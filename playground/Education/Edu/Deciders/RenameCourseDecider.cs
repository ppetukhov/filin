using Edu.ReadModels;
using Edu.UseCases;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Deciders;

/// <inheritdoc cref="IDecider{RenameCourse, ActiveCourses}"/>
[Decider]
sealed class RenameCourseDecider : IDecider<RenameCourse, ActiveCourses>
{
    /// <inheritdoc/>
    public static DecideResult Decide(RenameCourse command) => DomainErrors.Course.NotFound(command.CourseId);

    /// <inheritdoc/>
    public static DecideResult Decide(RenameCourse command, ActiveCourses state)
    {
        if (!state.Courses.TryGetValue(command.CourseId, out var currentCourseName))
        {
            return DomainErrors.Course.NotFound(command.CourseId);
        }

        if (StringComparer.Ordinal.Equals(currentCourseName, command.NewName))
        {
            return DomainErrors.Course.NameAlreadyCurrent(currentCourseName);
        }

        if (!StringComparer.Ordinal.Equals(currentCourseName, command.OldName))
        {
            return DomainErrors.Course.NameMismatch(command.CourseId, command.OldName);
        }

        bool Predicate(KeyValuePair<Guid, string> kv) => kv.Key != command.CourseId && StringComparer.OrdinalIgnoreCase.Equals(kv.Value, command.NewName);

        return state.Courses.FirstOrDefault(Predicate, defaultValue: new KeyValuePair<Guid, string>(default, string.Empty)) is { Value.Length: > 0 } existingCourse
            ? DomainErrors.Course.DuplicateName(existingCourse.Value, existingCourse.Key)
            : CourseRenamed(command, currentCourseName);
    }

    static DecideResult CourseRenamed(RenameCourse command, string currentName) => new V1.CourseRenamed(command.CourseId, command.NewName, OldName: currentName).ToDcbEvent(command.GetTags());
}
