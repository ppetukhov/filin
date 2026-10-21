namespace Filin.Internal;

/// <inheritdoc cref="IProjector{TState}"/>
interface IProjectorProxy<TState> where TState : notnull
{
    /// <inheritdoc cref="IProjector{TState}.ProjectorId"/>
    ProjectorId ProjectorId { get; }

    /// <inheritdoc cref="IProjector{TState}.GetQuery(Tags)"/>
    DcbQuery GetQuery(Tags tags);

    /// <inheritdoc cref="IProjector{TState}.GetProjectionId(Tags)"/>
    ProjectionId GetProjectionId(Tags tags);

    /// <inheritdoc cref="IProjector{TState}.Project(DcbEventSequenced)"/>
    TState Project(DcbEventSequenced domainEvent);

    /// <inheritdoc cref="IProjector{TState}.Project(TState, DcbEventSequenced)"/>
    TState Project(TState state, DcbEventSequenced domainEvent);
}
