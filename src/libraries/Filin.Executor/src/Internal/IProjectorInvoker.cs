namespace Filin.Internal;

/// <inheritdoc cref="IProjector{TState}"/>
interface IProjectorInvoker<TState> where TState : notnull
{
    /// <inheritdoc cref="IProjector{TState}.ProjectorId"/>
    ProjectorId ProjectorId { get; }

    /// <inheritdoc cref="IProjector{TState}.GetQuery(TagCollection)"/>
    DcbQuery GetQuery(TagCollection tags);

    /// <inheritdoc cref="IProjector{TState}.GetProjectionId(TagCollection)"/>
    ProjectionId GetProjectionId(TagCollection tags);

    /// <inheritdoc cref="IProjector{TState}.Project(DcbEventSequenced)"/>
    TState Project(DcbEventSequenced domainEvent);

    /// <inheritdoc cref="IProjector{TState}.Project(TState, DcbEventSequenced)"/>
    TState Project(TState state, DcbEventSequenced domainEvent);
}
