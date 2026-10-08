#pragma warning disable CA1034 // Do not nest type . Alternatively, change its accessibility so that it is not externally visible.

namespace Edu.ReadModels;

/// <summary>
/// Provides core validation metrics and status evaluations for domain read models.
/// </summary>
public static class ReadModelsExtensions
{
    extension(StudentCourseSubscriptionState state)
    {
        /// <summary>
        /// Gets a value indicating whether the student aggregate exists in the domain context.
        /// </summary>
        /// <returns><see langword="true"/> if the student exists; otherwise, <see langword="false"/>.</returns>
        public bool StudentExists => state.StudentId != default;

        /// <summary>
        /// Gets a value indicating whether the course aggregate exists in the domain context.
        /// </summary>
        /// <returns><see langword="true"/> if the course exists; otherwise, <see langword="false"/>.</returns>
        public bool CourseExists => state.CourseId != default;

        /// <summary>
        /// Gets a value indicating whether the target course has reached its maximum subscription capacity.
        /// </summary>
        /// <returns><see langword="true"/> if the course is full; otherwise, <see langword="false"/>.</returns>
        public bool IsCourseFull => state.CourseCapacity != default && state.CourseSubscriptionCount >= state.CourseCapacity;

        /// <summary>
        /// Gets a value indicating whether the student is already actively subscribed to the target course.
        /// </summary>
        /// <returns><see langword="true"/> if the student is already subscribed; otherwise, <see langword="false"/>.</returns>
        public bool IsAlreadySubscribed => state.SubscribedCourseIds.Contains(state.CourseId);
    }
}
