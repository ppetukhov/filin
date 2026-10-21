namespace Filin;

/// <summary>
/// Associates a specific query identifier with event filter tag parameters or tag prefixes.
/// </summary>
/// <remarks>
/// The query is identified by the <see cref="QueryId"/> property value.
/// </remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="tagParameters"><inheritdoc cref="TagParameters" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryTagParametersAttribute(int queryId, params string[] tagParameters) : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTagParametersAttribute"/> class with a default query identifier.
    /// </summary>
    /// <param name="tagParameters"><inheritdoc cref="TagParameters" path="/summary"/></param>
    public QueryTagParametersAttribute(params string[] tagParameters) : this(queryId: default, tagParameters: tagParameters) { }

    /// <summary>
    /// Gets the unique identifier of the query.
    /// </summary>
    /// <value>The default value is 0.</value>
    public int QueryId => queryId;

    /// <summary>
    /// Gets the event filter tag parameters or tag prefixes.
    /// </summary>
    public IEnumerable<string> TagParameters => tagParameters;
}
