using Edu.ReadModels;

using Filin;

namespace Edu.UseCases;

/// <summary>
/// Creates a new course.
/// </summary>
/// <param name="CourseId">The unique identifier of the course.</param>
/// <param name="Name">The name of the course.</param>
public sealed partial record CreateCourse([EventTag("course")] Guid CourseId, string Name) : ICommand<ActiveCourses>;
