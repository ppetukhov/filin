namespace Filin;

/// <summary>
/// Maps a specific query identifier to its allowed or produced event types.
/// </summary>
/// <remarks>
/// The query is identified by the <see cref="QueryId"/> property value.
/// </remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="eventTypes"><inheritdoc cref="EventTypes" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryEventTypesAttribute(int queryId, params string[] eventTypes) : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryEventTypesAttribute"/> class with a default query identifier.
    /// </summary>
    /// <param name="eventTypes"><inheritdoc cref="EventTypes" path="/summary"/></param>
    public QueryEventTypesAttribute(params string[] eventTypes) : this(queryId: default, eventTypes: eventTypes) { }

    /// <summary>
    /// Gets the unique identifier of the query.
    /// </summary>
    /// <value>The default value is 0.</value>
    public int QueryId => queryId;

    /// <summary>
    /// Gets the collection of event types associated with the query.
    /// </summary>
    public IEnumerable<string> EventTypes => eventTypes;
}
