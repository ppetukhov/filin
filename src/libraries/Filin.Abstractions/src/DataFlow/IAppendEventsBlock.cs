using System.Threading.Tasks.Dataflow;

using Filin.Storage;

namespace Filin.DataFlow;

public record struct AppendEventRequest(DcbEvent Event, AppendCondition AppendCondition, IContext Context);
public record struct AppendEventsRequest(ICollection<DcbEvent> Events, AppendCondition AppendCondition, IContext Context);
public record struct AppendEventsResponse(AppendResult Result, IContext Context);

public interface IContext
{
    CancellationToken CancellationToken { get; }

    bool TrySetCanceled(CancellationToken cancellationToken);
    bool TrySetException(Exception exception);
}

public interface IAppendEventsBlock : IPropagatorBlock<AppendEventRequest, AppendEventsResponse>, IPropagatorBlock<AppendEventsRequest, AppendEventsResponse>
{
}
