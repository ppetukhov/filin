using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Edu.ReadModels;

/// <summary>
/// Represents the domain read-state containing all active courses.
/// </summary>
public sealed record ActiveCourses
{
    /// <summary>
    /// Gets the collection of active courses mapped by their unique identifiers and names.
    /// </summary>
    [JsonPropertyName("courses")]
    public required ImmutableDictionary<Guid, string> Courses { get; init; }
}
