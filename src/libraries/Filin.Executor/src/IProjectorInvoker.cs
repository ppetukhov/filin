using System.Collections.Immutable;

namespace Filin;

/// <inheritdoc cref="IProjector{TState}"/>
interface IProjectorInvoker<TState> where TState : notnull
{
    /// <inheritdoc cref="IProjector{TState}.ProjectorId"/>
    ProjectorId ProjectorId { get; }

    /// <inheritdoc cref="IProjector{TState}.GetQuery(ImmutableHashSet{Tag})"/>
    DcbQuery GetQuery(ImmutableHashSet<Tag> tags);

    /// <inheritdoc cref="IProjector{TState}.GetProjectionId(ImmutableHashSet{Tag})"/>
    ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags);

    /// <inheritdoc cref="IProjector{TState}.Project(DcbEventSequenced)"/>
    TState Project(DcbEventSequenced domainEvent);

    /// <inheritdoc cref="IProjector{TState}.Project(TState, DcbEventSequenced)"/>
    TState Project(TState state, DcbEventSequenced domainEvent);
}
