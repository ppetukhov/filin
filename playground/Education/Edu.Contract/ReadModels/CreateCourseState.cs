using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Edu.ReadModels;

/// <summary>
/// Represents the domain read-state of active cource id & name.
/// </summary>
public sealed record CreateCourseState
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? CourseId { get; init; }

    /// <summary>
    /// Current course name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? CourseName { get; init; }

    [JsonPropertyName("exist_names")]
    public ImmutableHashSet<string>? ExistNames { get; init; }
}
