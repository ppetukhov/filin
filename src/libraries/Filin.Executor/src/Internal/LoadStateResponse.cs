namespace Filin.Internal;

sealed record LoadStateResponse<TState>(TState? State, StateMetadata Metadata, LoadStateRequest<TState> LoadRequest)
    where TState : notnull;
