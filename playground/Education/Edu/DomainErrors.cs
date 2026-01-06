using Filin;

namespace Edu;

/// <summary>
/// Centralized factory for all domain and business errors.
/// </summary>
static class DomainErrors
{
    internal static class Student
    {
        internal static DecideResult NotFound(Guid studentId) => ("STUDENT_NOT_FOUND", $"Student with ID '{studentId}' was not found.");

        internal static DecideResult SubscriptionLimitReached(Guid studentId, uint maxLimit) => ("STUDENT_SUBSCRIPTION_LIMIT_REACHED", $"Student with ID '{studentId}' has reached the maximum limit of {maxLimit} subscriptions.");

        internal static DecideResult AlreadySubscribed(Guid studentId, Guid courseId) => ("STUDENT_ALREADY_SUBSCRIBED", $"Student with ID '{studentId}' is already subscribed to course with ID '{courseId}'.");
    }

    internal static class Course
    {
        internal static DecideResult NotFound(Guid courseId) => ("COURSE_NOT_FOUND", $"Course with ID '{courseId}' was not found.");

        internal static DecideResult AlreadyExists(string existCourseName, Guid courseId) => ("COURSE_ALREADY_EXISTS", $"Course '{existCourseName}' with ID '{courseId}' already exists.");

        internal static DecideResult DuplicateName(string duplicateCourseName, Guid existingCourseId) => ("DUPLICATE_COURSE_NAME", $"Course '{duplicateCourseName}' already exists with ID '{existingCourseId}'.");

        internal static DecideResult NameAlreadyCurrent(string currentCourseName) => ("COURSE_NAME_ALREADY_CURRENT", $"The course already has the name '{currentCourseName}'.");

        internal static DecideResult NameMismatch(Guid courseId, string courseOldName) => ("COURSE_NAME_MISMATCH", $"Cannot rename course '{courseId}' because its current name does not match the expected source name '{courseOldName}'.");

        internal static DecideResult SubscriptionLimitReached(Guid courseId, uint maxCapacity) => ("COURSE_SUBSCRIPTION_LIMIT_REACHED", $"Course with ID '{courseId}' is full. The maximum limit of {maxCapacity} subscriptions has been reached.");
    }
}
