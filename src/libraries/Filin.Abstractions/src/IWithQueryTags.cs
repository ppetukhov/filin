namespace Filin;

/// <summary>
/// Defines a contract for domain objects that expose a collection of query tags to form consistency boundaries.
/// </summary>
public interface IWithQueryTags
{
    /// <summary>
    /// Gets the guaranteed sorted, immutable collection of query tags used to evaluate dynamic consistency boundaries.
    /// </summary>
    /// <value>
    /// A <see cref="Tags"/> containing the pre-sorted query tags and their combined hash.
    /// </value>
    Tags GetTags();
}
