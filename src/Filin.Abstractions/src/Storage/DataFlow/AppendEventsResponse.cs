namespace Filin.Storage.DataFlow;

/// <summary>
/// Represents the result payload of an append operation.
/// </summary>
/// <param name="Result">The detailed status or outcome of the append operation.</param>
/// <param name="Context">The propagated execution context associated with the request.</param>
public record struct AppendEventsResponse(AppendResult Result, IContext Context);
