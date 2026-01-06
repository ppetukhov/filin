namespace Filin;

/// <summary>
/// Типы событий.
/// </summary>
/// <remarks>Запрос идентифицирутся по значению <see cref="QueryId"/>.</remarks>
/// <param name="queryId"><inheritdoc cref="QueryId" path="/summary"/></param>
/// <param name="eventTypes"><inheritdoc cref="EventTypes" path="/summary"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class QueryEventTypesAttribute(int queryId, params string[] eventTypes) : Attribute
{
    /// <inheritdoc cref="QueryEventTypesAttribute" path="/summary"/>
    /// <param name="eventTypes"><inheritdoc cref="EventTypes" path="/summary"/></param>
    public QueryEventTypesAttribute(params string[] eventTypes) : this(queryId: default, eventTypes: eventTypes) { }

    /// <summary>
    /// Номер фильтра.
    /// </summary>
    /// <remarks>Значение по умолчанию 0</remarks>
    public int QueryId { get; } = queryId;

    /// <summary>
    /// Типы событий.
    /// </summary>
    public IEnumerable<string> EventTypes { get; } = eventTypes;
}
