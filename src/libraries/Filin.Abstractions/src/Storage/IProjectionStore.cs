namespace Filin.Storage;

/// <summary>
/// Defines the storage contract for retrieving, persisting, and clearing read model projections.
/// </summary>
public interface IProjectionStore
{
    /// <summary>
    /// Asynchronously loads a specific projection by its identifier.
    /// </summary>
    /// <param name="projectionId"><inheritdoc cref="ProjectionId" path="/summary"/></param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="Projection"/> if found; 
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="DcbProjectionStoreException">Thrown when a storage error occurs while retrieving the projection.</exception>
    Task<Projection?> LoadAsync(ProjectionId projectionId, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously saves or updates a projection in the store.
    /// </summary>
    /// <param name="projection"><inheritdoc cref="Projection" path="/summary"/></param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="DcbProjectionStoreException">Thrown when a storage error occurs while persisting the projection.</exception>
    Task SaveAsync(Projection projection, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously saves a projection with an option to strictly enforce its creation.
    /// </summary>
    /// <param name="projection"><inheritdoc cref="Projection" path="/summary"/></param>
    /// <param name="create"><see langword="true"/> to strictly insert a new projection record; <see langword="true"/> to allow upsert or modification behavior.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="DcbProjectionStoreException">Thrown when a storage error or concurrency constraint violation occurs while persisting the projection.</exception>
    Task SaveAsync(Projection projection, bool create, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously saves a batch of projections atomically or as a single bulk operation.
    /// </summary>
    /// <param name="projections">The collection of projections to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="DcbProjectionStoreException">Thrown when a storage error occurs during the bulk persist operation.</exception>
    Task SaveAsync(IEnumerable<Projection> projections, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously clears or deletes the projection data, rolling it back up to a specific event stream position.
    /// </summary>
    /// <param name="projectionId">The unique identifier of the projection to be cleared.</param>
    /// <param name="position">The stream or global event position associated with this clearing/reset operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="DcbProjectionStoreException">Thrown when an error occurs while clearing the projection data.</exception>
    Task ClearAsync(ProjectionId projectionId, EventPosition position, CancellationToken cancellationToken);
}
