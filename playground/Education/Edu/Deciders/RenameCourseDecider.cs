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
            return DomainErrors.Course.DuplicateName(command.NewName);
        }

        return CourseRenamed(command, state.CourseName);
    }

    static DecideResult CourseRenamed(RenameCourse command, string currentName) =>
        new V1.CourseRenamed(command.CourseId, command.NewName, OldName: currentName).ToDcbEvent(command.GetTags());
}
