using System.Threading.Tasks.Dataflow;

using Filin.Storage.DataFlow;

namespace Filin.Tests;

sealed class StubAppendEventsBlock : IAppendEventsBlock
{
    readonly BufferBlock<AppendEventsResponse> _outputBlock = new();
    readonly BufferBlock<AppendEventRequest> _inputSingleBlock = new();
    readonly BufferBlock<AppendEventsRequest> _inputBatchBlock = new();

    public IDisposable LinkTo(ITargetBlock<AppendEventsResponse> target, DataflowLinkOptions linkOptions) =>
        _outputBlock.LinkTo(target, linkOptions);

    AppendEventsResponse ISourceBlock<AppendEventsResponse>.ConsumeMessage(
        DataflowMessageHeader messageHeader,
        ITargetBlock<AppendEventsResponse> target,
        out bool messageConsumed) =>
        ((ISourceBlock<AppendEventsResponse>)_outputBlock).ConsumeMessage(messageHeader, target, out messageConsumed);

    bool ISourceBlock<AppendEventsResponse>.ReserveMessage(
        DataflowMessageHeader messageHeader,
        ITargetBlock<AppendEventsResponse> target) =>
        ((ISourceBlock<AppendEventsResponse>)_outputBlock).ReserveMessage(messageHeader, target);

    void ISourceBlock<AppendEventsResponse>.ReleaseReservation(
        DataflowMessageHeader messageHeader,
        ITargetBlock<AppendEventsResponse> target) =>
        ((ISourceBlock<AppendEventsResponse>)_outputBlock).ReleaseReservation(messageHeader, target);

    DataflowMessageStatus ITargetBlock<AppendEventRequest>.OfferMessage(
        DataflowMessageHeader messageHeader,
        AppendEventRequest messageValue,
        ISourceBlock<AppendEventRequest>? source,
        bool consumeToAccept) =>
        ((ITargetBlock<AppendEventRequest>)_inputSingleBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);

    DataflowMessageStatus ITargetBlock<AppendEventsRequest>.OfferMessage(
        DataflowMessageHeader messageHeader,
        AppendEventsRequest messageValue,
        ISourceBlock<AppendEventsRequest>? source,
        bool consumeToAccept) =>
        ((ITargetBlock<AppendEventsRequest>)_inputBatchBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);

    public Task Completion => _outputBlock.Completion;
    public void Complete() => _outputBlock.Complete();
    public void Fault(Exception exception) => ((IDataflowBlock)_outputBlock).Fault(exception);
}
