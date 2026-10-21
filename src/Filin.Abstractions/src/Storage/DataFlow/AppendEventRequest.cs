namespace Filin.Storage.DataFlow;

/// <summary>
/// Represents a request to append a single domain event.
/// </summary>
/// <param name="Event">The domain event payload to append.</param>
/// <param name="AppendCondition">The optimistic concurrency or execution conditions for the append operation.</param>
/// <param name="Context">The execution context managing lifecycle and cancellation tokens.</param>
public record struct AppendEventRequest(DcbEvent Event, AppendCondition AppendCondition, IContext Context);
