namespace Filin.ProjectionStore.AdoNet;

/// <summary>
/// A container class for the queries currently used by the <see cref="ProjectionStore"/>.
/// </summary>
/// <remarks>This is provided as a separate entity in order to make these dynamically updatable.</remarks>
sealed record StorageQueries
{
    /// <summary>
    /// The clause to read from the storage.
    /// </summary>
    public required string ReadStorageKey { get; init; }

    /// <summary>
    /// The clause to write to the storage.
    /// </summary>
    public required string UpsertStorageKey { get; init; }

    /// <summary>
    /// The clause to write to the storage.
    /// </summary>
    public required string UpsertBatchStorageKey { get; init; }

    /// <summary>
    /// The clause to clear the storage.
    /// </summary>
    public required string ClearStorageKey { get; init; }
}
