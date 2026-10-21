using System.Threading.Tasks.Dataflow;

namespace Filin.Storage.DataFlow;

/// <summary>
/// Defines a dataflow propagator block capable of processing both single and batched event append requests, 
/// producing a consolidated append operation response.
/// </summary>
/// <remarks>
/// This block acts as both a target for incoming requests (<see cref="AppendEventRequest"/> or <see cref="AppendEventsRequest"/>) 
/// and a source for outgoing operation execution responses (<see cref="AppendEventsResponse"/>).
/// </remarks>
public interface IAppendEventsBlock : IPropagatorBlock<AppendEventRequest, AppendEventsResponse>, IPropagatorBlock<AppendEventsRequest, AppendEventsResponse>
{
}
