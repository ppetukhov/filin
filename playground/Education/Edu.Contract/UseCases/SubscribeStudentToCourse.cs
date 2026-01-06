using Edu.ReadModels;

using Filin;

namespace Edu.UseCases;

/// <summary>
/// Represents a command to subscribe a student to a specific course.
/// </summary>
/// <param name="CourseId">The unique identifier of the target course.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
public sealed partial record SubscribeStudentToCourse([EventTag("course")] Guid CourseId, [EventTag("student")] Guid StudentId) : ICommand<StudentCourseSubscriptionState>;
