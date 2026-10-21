namespace Filin;

/// <summary>
/// Defines a domain query representing an intent to retrieve a read model projection of type <typeparamref name="TProjection"/>.
/// </summary>
/// <typeparam name="TProjection">The type of the read model projection returned by the query.</typeparam>
public interface IQuery<TProjection> : IWithQueryTags where TProjection : notnull;
