namespace Filin;

/// <summary>
/// Тэги событий запроса.
/// </summary>
/// <remarks>Запрос идентифицирутся по значению <see cref="QueryId"/>.</remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="eventTags"><inheritdoc cref="EventTags" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryTagsAttribute(int queryId, params string[] eventTags) : Attribute
{
    /// <inheritdoc cref="QueryTagsAttribute" path="/summary"/>
    /// <param name="eventTags"><inheritdoc cref="EventTags" path="/summary"/></param>
    public QueryTagsAttribute(params string[] eventTags) : this(queryId: default, eventTags: eventTags) { }

    /// <summary>
    /// Номер фильтра.
    /// </summary>
    /// <remarks>Значение по умолчанию 0</remarks>
    public int QueryId { get; } = queryId;

    /// <summary>
    /// Тэги событий.
    /// </summary>
    public IEnumerable<string> EventTags { get; } = eventTags;
}
