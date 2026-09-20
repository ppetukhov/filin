using Edu.ReadModels;

using Filin;

namespace Edu.UseCases;

/// <summary>
/// Represents a command to rename an existing course.
/// </summary>
/// <param name="CourseId">The unique identifier of the course being renamed.</param>
/// <param name="NewName">The new target name for the course.</param>
/// <param name="OldName">The expected previous name of the course for concurrency validation.</param>
public sealed partial record RenameCourse([EventTag("course")] Guid CourseId, [EventTag("course_name")] string NewName, string OldName) : ICommand<CreateCourseState>;
