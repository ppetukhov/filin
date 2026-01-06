using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Edu.ReadModels;

/// <summary>
/// Holds the consolidated read-state required to validate a student's subscription to a specific course.
/// </summary>
public sealed record StudentCourseSubscriptionState
{
    /// <summary>
    /// Gets the unique identifier of the student.
    /// </summary>
    [JsonPropertyName("student_id")]
    public required Guid StudentId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the target course.
    /// </summary>
    [JsonPropertyName("course_id")]
    public required Guid CourseId { get; init; }

    /// <summary>
    /// Gets the set of course IDs the student is currently subscribed to.
    /// </summary>
    [JsonPropertyName("subscribed_course_ids")]
    public ImmutableHashSet<Guid> SubscribedCourseIds { get; init; } = [];

    /// <summary>
    /// Gets the maximum subscriber capacity of the target course.
    /// </summary>
    [JsonPropertyName("course_capacity")]
    public uint CourseCapacity { get; init; }

    /// <summary>
    /// Gets the total number of active subscriptions for the target course.
    /// </summary>
    [JsonPropertyName("course_subscription_count")]
    public uint CourseSubscriptionCount { get; init; }
}
