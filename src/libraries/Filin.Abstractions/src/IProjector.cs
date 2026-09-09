using System.Collections.Immutable;

namespace Filin;

/// <summary>
/// Defines a contract for an event projector that flattens domain events into a read model 
/// used for business validation and decision-making.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be marked with infrastructure attributes to declare metadata 
/// if automatic registration is required.
/// </para>
/// <para>
/// This interface utilizes static abstract members to allow metadata resolution 
/// and query building directly at the type level without instantiation overhead.
/// </para>
///
/// <para>
/// The <see cref="ProjectorIdAttribute"/> is used to declare a unique runtime identifier for the projector.
/// </para>
/// <para>
/// The <see cref="QueryEventTypesAttribute"/> and <see cref="QueryTagsAttribute"/> are utilized 
/// by the infrastructure framework to automatically discover and map metadata filtering rules for this projector.
/// </para>
/// </remarks>
/// <typeparam name="TState">The immutable type of the projection state.</typeparam>
public interface IProjector<TState> where TState : notnull
{
    /// <summary>
    /// Gets the unique identifier of the projector type.
    /// </summary>
    static abstract ProjectorId ProjectorId { get; }

    /// <summary>
    /// Builds a deterministic query containing sorted tags and interested event types for history retrieval.
    /// </summary>
    /// <param name="tags">The zero-allocation collection of tags from the current execution context.</param>
    /// <returns>A <see cref="DcbQuery"/> defining the dynamic consistency boundary for this projection.</returns>
    /// <exception cref="ArgumentException">Thrown when a required tag is missing from the provided collection.</exception>
    static abstract DcbQuery GetQuery(ImmutableSortedSet<Tag> tags);

    /// <summary>
    /// Generates a unique composite projection identity based on the execution context tags.
    /// </summary>
    /// <param name="tags">The zero-allocation collection of tags from the current execution context.</param>
    /// <returns>The unique identity of the projection.</returns>
    /// <exception cref="ArgumentException">Thrown when a required tag is missing from the provided collection.</exception>
    static abstract ProjectionId GetProjectionId(ImmutableSortedSet<Tag> tags);

    /// <summary>
    /// Initializes the projection state from the very first event in the history stream.
    /// </summary>
    /// <param name="domainEvent">The first sequenced domain event retrieved from the store.</param>
    /// <returns>The newly initialized <typeparamref name="TState"/> instance.</returns>
    /// <exception cref="DcbProjectorException">Thrown when an infrastructure or projection mapping failure occurs.</exception>
    static abstract TState Project(DcbEventSequenced domainEvent);

    /// <summary>
    /// Incrementally updates (evolves) the existing projection state by applying a subsequent event.
    /// </summary>
    /// <param name="state">The current snapshot of the projection state.</param>
    /// <param name="domainEvent">The subsequent sequenced domain event to apply.</param>
    /// <returns>The evolved <typeparamref name="TState"/> instance.</returns>
    /// <exception cref="DcbProjectorException">Thrown when an infrastructure or projection mapping failure occurs.</exception>
    static abstract TState Project(TState state, DcbEventSequenced domainEvent);
}
