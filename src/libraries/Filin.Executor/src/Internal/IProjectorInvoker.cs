using System.Collections.Immutable;

namespace Filin.Internal;

/// <inheritdoc cref="IProjector{TState}"/>
interface IProjectorInvoker<TState> where TState : notnull
{
    /// <inheritdoc cref="IProjector{TState}.ProjectorId"/>
    ProjectorId ProjectorId { get; }

    /// <inheritdoc cref="IProjector{TState}.GetQuery(ImmutableSortedSet{Tag})"/>
    DcbQuery GetQuery(ImmutableSortedSet<Tag> tags);

    /// <inheritdoc cref="IProjector{TState}.GetProjectionId(ImmutableSortedSet{Tag})"/>
    ProjectionId GetProjectionId(ImmutableSortedSet<Tag> tags);

    /// <inheritdoc cref="IProjector{TState}.Project(DcbEventSequenced)"/>
    TState Project(DcbEventSequenced domainEvent);

    /// <inheritdoc cref="IProjector{TState}.Project(TState, DcbEventSequenced)"/>
    TState Project(TState state, DcbEventSequenced domainEvent);
}
