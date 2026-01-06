using System.Collections.Immutable;

namespace Filin;

/// <inheritdoc cref="IProjectorInvoker{TState}"/>
sealed class ProjectorInvoker<TProjector, TState> : IProjectorInvoker<TState>
    where TProjector : IProjector<TState>
    where TState : notnull
{
    /// <inheritdoc/>
    public ProjectorId ProjectorId => TProjector.ProjectorId;

    /// <inheritdoc/>
    public DcbQuery GetQuery(ImmutableHashSet<Tag> tags) => TProjector.GetQuery(tags);

    /// <inheritdoc/>
    public ProjectionId GetProjectionId(ImmutableHashSet<Tag> tags) => TProjector.GetProjectionId(tags);

    /// <inheritdoc/>
    public TState Project(DcbEventSequenced domainEvent) => TProjector.Project(domainEvent);

    /// <inheritdoc/>
    public TState Project(TState state, DcbEventSequenced domainEvent) => TProjector.Project(state, domainEvent);
}
