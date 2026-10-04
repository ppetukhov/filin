namespace Filin;

/// <summary>
/// Defines a domain query representing an intent to retrieve a read model projection of type <typeparamref name="TProjection"/>.
/// </summary>
/// <typeparam name="TProjection">The type of the read model projection returned by the query.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Идентификация запроса должна выполняться во время компиляции.")]
public interface IQuery<TProjection> : IWithQueryTags where TProjection : notnull;
