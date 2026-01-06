using Edu.ReadModels;
using Edu.UseCases;

using Filin;

using V1 = Edu.DomainEvents.V1;

namespace Edu.Deciders;

/// <inheritdoc cref="IDecider{CreateCourse, ActiveCourses}"/>
[Decider]
sealed class CreateCourseDecider : IDecider<CreateCourse, ActiveCourses>
{
    /// <inheritdoc/>
    public static DecideResult Decide(CreateCourse command) => CourseCreated(command);

    /// <inheritdoc/>
    public static DecideResult Decide(CreateCourse command, ActiveCourses state)
    {
        if (state.Courses.TryGetValue(command.CourseId, out var existCourseName))
        {
            return DomainErrors.Course.AlreadyExists(existCourseName, command.CourseId);
        }

        bool Predicate(KeyValuePair<Guid, string> kv) => StringComparer.OrdinalIgnoreCase.Equals(kv.Value, command.Name);

        if (state.Courses.FirstOrDefault(Predicate, defaultValue: new KeyValuePair<Guid, string>(default, string.Empty)) is { Value.Length: > 0 } existingCourse)
        {
            return DomainErrors.Course.DuplicateName(existingCourse.Value, existingCourse.Key);
        }

        return CourseCreated(command);
    }

    static DecideResult CourseCreated(CreateCourse command) => new V1.CourseCreated(command.CourseId, command.Name).ToDcbEvent(command.GetTags());
}
