namespace Filin.ProjectionStore.AdoNet;

/// <summary>
/// A container class for the queries currently used by the <see cref="ProjectionStore"/>.
/// </summary>
/// <remarks>This is provided as a separate entity in order to make these dynamically updatable.</remarks>
public sealed record RelationalStorageProviderQueries
{
    /// <summary>
    /// The clause to create record in the storage.
    /// </summary>
    public required string CreateStorageKey { get; init; }

    /// <summary>
    /// The clause to read from the storage.
    /// </summary>
    public required string ReadStorageKey { get; init; }

    /// <summary>
    /// The clause to write to the storage.
    /// </summary>
    public required string UpdateStorageKey { get; init; }

    /// <summary>
    /// The clause to clear the storage.
    /// </summary>
    public required string ClearStorageKey { get; init; }
}
