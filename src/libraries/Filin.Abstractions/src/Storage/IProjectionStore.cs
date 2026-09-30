namespace Filin.Storage;

/// <summary>
/// Хранилище проекций.
/// </summary>
public interface IProjectionStore
{
    /// <summary>
    /// Загрузить проекцию.
    /// </summary>
    /// <param name="projectionId"><inheritdoc cref="ProjectionId" path="/summary"/></param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns><see langword="null"/> если проекция не найдена.</returns>
    Task<Projection?> LoadAsync(ProjectionId projectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить проекцию.
    /// </summary>
    /// <param name="projection"><inheritdoc cref="Projection" path="/summary"/></param>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    Task SaveAsync(Projection projection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить проекцию.
    /// </summary>
    /// <param name="projection"><inheritdoc cref="Projection" path="/summary"/></param>
    /// <param name="create">Создать запись проекции.</param>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    Task SaveAsync(Projection projection, bool create, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить проекции.
    /// </summary>
    /// <param name="projections"><inheritdoc cref="Projection" path="/summary"/></param>
    /// <exception cref="DcbProjectionStoreException"/>
    /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    Task SaveAsync(IEnumerable<Projection> projections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить/очистить данные проекции.
    /// </summary>
    /// <param name="projectionId"></param>
    /// <param name="position"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ClearAsync(ProjectionId projectionId, EventPosition position, CancellationToken cancellationToken = default);
}
