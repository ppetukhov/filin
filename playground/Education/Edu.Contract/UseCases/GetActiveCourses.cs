using System.Collections.Immutable;

using Edu.ReadModels;

using Filin;

namespace Edu.UseCases;

/// <summary>
/// Represents a query request to retrieve all active courses.
/// </summary>
public sealed record GetActiveCourses : IQuery<ActiveCourses>
{
    /// <inheritdoc/>
    public ImmutableSortedSet<Tag> GetTags() => [];
}
