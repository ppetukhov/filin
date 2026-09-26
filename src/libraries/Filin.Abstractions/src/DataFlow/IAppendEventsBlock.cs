using System.Threading.Tasks.Dataflow;

using Filin.Storage;

namespace Filin.DataFlow;

public record struct AppendEventRequest(DcbEvent Event, AppendCondition AppendCondition, object Context, CancellationToken CancellationToken);
public record struct AppendEventsRequest(ICollection<DcbEvent> Events, AppendCondition AppendCondition, object Context, CancellationToken CancellationToken);
public record struct AppendEventsResponse(AppendResult Result, object Context);

public interface IAppendEventsBlock : IPropagatorBlock<AppendEventRequest, AppendEventsResponse>, IPropagatorBlock<AppendEventsRequest, AppendEventsResponse>
{
}
