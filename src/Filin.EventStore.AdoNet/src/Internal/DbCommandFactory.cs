using System.Data.Common;

using Filin.Storage;

namespace Filin.EventStore.AdoNet.Internal;

abstract class DbCommandFactory
{
    internal abstract DbCommand CreateAppendEventCommand(DcbEvent @event, AppendCondition appendCondition);
    internal abstract DbCommand CreateAppendEventsCommand(IReadOnlyCollection<DcbEvent> events, AppendCondition appendCondition);
    internal abstract DbCommand CreateReadEventsCommand(EventPosition afterPosition, DcbQuery query, int limit);
}
