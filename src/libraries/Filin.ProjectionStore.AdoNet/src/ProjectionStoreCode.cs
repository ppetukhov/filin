namespace Filin.ProjectionStore.AdoNet;

/// <summary>
/// Logging codes used by <see cref="ProjectionStore"/>.
/// </summary>
enum ProjectionStoreCode
{
    ProvidersBase = 200000,
    ProjectionStoreBase = ProvidersBase + 400,

    ProjectionInitStore = ProjectionStoreBase + 1,

    ProjectionReading = ProjectionStoreBase + 11,
    ProjectionRead = ProjectionStoreBase + 12,
    ProjectionNoStateFound = ProjectionStoreBase + 13,
    ProjectionNoDeserializer = ProjectionStoreBase + 14,
    ProjectionReadError = ProjectionStoreBase + 15,

    ProjectionWriting = ProjectionStoreBase + 21,
    ProjectionWrote = ProjectionStoreBase + 22,
    ProjectionWriteError = ProjectionStoreBase + 23,

    ProjectionClearing = ProjectionStoreBase + 31,
    ProjectionCleared = ProjectionStoreBase + 32,
    ProjectionClearingError = ProjectionStoreBase + 33,

    ProjectionBatchWriting = ProjectionStoreBase + 34,
    ProjectionBatchWrote = ProjectionStoreBase + 35,
    ProjectionBatchWriteError = ProjectionStoreBase + 36,
}
