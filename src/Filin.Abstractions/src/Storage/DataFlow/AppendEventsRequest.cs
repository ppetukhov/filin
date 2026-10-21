namespace Filin.Storage.DataFlow;

/// <summary>
/// Represents a request to append a batch of domain events atomically.
/// </summary>
/// <param name="Events">The collection of domain event payloads to append.</param>
/// <param name="AppendCondition">The optimistic concurrency or execution conditions for the append operation.</param>
/// <param name="Context">The execution context managing lifecycle and cancellation tokens.</param>
public readonly record struct AppendEventsRequest(ICollection<DcbEvent> Events, AppendCondition AppendCondition, IContext Context);
