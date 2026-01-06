namespace Filin;

/// <summary>
/// Запрос на получение проекции <typeparamref name="TProjection"/>.
/// </summary>
/// <typeparam name="TProjection">Тип данных проекции.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Идентификация запроса должна выполняться во время компиляции.")]
public interface IQuery<TProjection> : IWithQueryTags where TProjection : notnull;
