namespace Filin.ProjectionStore.AdoNet;

/// <summary>
/// Logging codes used by <see cref="ProjectionStore"/>.
/// </summary>
enum ProjectionStoreCode
{
    ProjectionStoreBase = 200,

    StorageInitialized = ProjectionStoreBase + 1,

    StateReading = ProjectionStoreBase + 11,
    StateRead = ProjectionStoreBase + 12,
    StateNotFound = ProjectionStoreBase + 13,
    NoDeserializer = ProjectionStoreBase + 14,
    StateReadError = ProjectionStoreBase + 15,

    StateWriting = ProjectionStoreBase + 21,
    StateWrote = ProjectionStoreBase + 22,
    StateWriteError = ProjectionStoreBase + 23,

    StateClearing = ProjectionStoreBase + 31,
    StateCleared = ProjectionStoreBase + 32,
    StateClearingError = ProjectionStoreBase + 33,

    StateBatchWriting = ProjectionStoreBase + 34,
    StateBatchWrote = ProjectionStoreBase + 35,
    StateBatchWriteError = ProjectionStoreBase + 36,
}
