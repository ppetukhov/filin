namespace Filin;

/// <summary>
/// Тэг-параметеры фильтра событий. (Префиксы тэгов).
/// </summary>
/// <remarks>Запрос идентифицирутся по значению <see cref="QueryId"/>.</remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="tagParameters"><inheritdoc cref="TagParameters" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryTagParametersAttribute(int queryId, params string[] tagParameters) : Attribute
{
    /// <inheritdoc cref="QueryTagsAttribute" path="/summary"/>
    /// <param name="tagParameters"><inheritdoc cref="QueryTagParametersAttribute" path="/summary"/></param>
    public QueryTagParametersAttribute(params string[] tagParameters) : this(queryId: default, tagParameters: tagParameters) { }

    /// <summary>
    /// Номер фильтра.
    /// </summary>
    /// <remarks>Значение по умолчанию 0</remarks>
    public int QueryId { get; } = queryId;

    /// <summary>
    /// Тэг-параметеры фильтра событий. (Префиксы тэгов).
    /// </summary>
    public IEnumerable<string> TagParameters { get; } = tagParameters;
}
