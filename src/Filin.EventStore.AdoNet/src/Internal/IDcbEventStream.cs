namespace Filin.EventStore.AdoNet.Internal;

interface IDcbEventStream
{
    bool IsActive { get; }
    DcbQuery Query { get; }
    bool CanAdd(DcbEventSequenced eventSequenced);
    ValueTask AddUnsafe(DcbEventSequenced eventSequenced);
}
