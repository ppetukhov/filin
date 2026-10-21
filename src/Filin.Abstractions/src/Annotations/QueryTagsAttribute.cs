namespace Filin;

/// <summary>
/// Associates a specific query identifier with event metadata tags.
/// </summary>
/// <remarks>
/// The query is identified by the <see cref="QueryId"/> property value.
/// </remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="eventTags"><inheritdoc cref="EventTags" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryTagsAttribute(int queryId, params string[] eventTags) : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTagsAttribute"/> class with a default query identifier.
    /// </summary>
    /// <param name="eventTags"><inheritdoc cref="EventTags" path="/summary"/></param>
    public QueryTagsAttribute(params string[] eventTags) : this(queryId: default, eventTags: eventTags) { }

    /// <summary>
    /// Gets the identifier of the query.
    /// </summary>
    /// <value>The default value is 0.</value>
    public int QueryId => queryId;

    /// <summary>
    /// Gets the collection of event metadata tags associated with the query.
    /// </summary>
    public IEnumerable<string> EventTags => eventTags;
}
