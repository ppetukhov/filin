using System.Collections.Immutable;

namespace Filin.Internal;

/// <inheritdoc cref="IProjectorInvoker{TState}"/>
sealed class ProjectorInvoker<TProjector, TState> : IProjectorInvoker<TState>
    where TProjector : IProjector<TState>
    where TState : notnull
{
    /// <inheritdoc/>
    public ProjectorId ProjectorId => TProjector.ProjectorId;

    /// <inheritdoc/>
    public DcbQuery GetQuery(ImmutableSortedSet<Tag> tags) => TProjector.GetQuery(tags);

    /// <inheritdoc/>
    public ProjectionId GetProjectionId(ImmutableSortedSet<Tag> tags) => TProjector.GetProjectionId(tags);

    /// <inheritdoc/>
    public TState Project(DcbEventSequenced domainEvent) => TProjector.Project(domainEvent);

    /// <inheritdoc/>
    public TState Project(TState state, DcbEventSequenced domainEvent) => TProjector.Project(state, domainEvent);
}
