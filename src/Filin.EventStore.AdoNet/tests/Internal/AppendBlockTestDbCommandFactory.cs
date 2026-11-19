using System.Data.Common;

using Filin.EventStore.AdoNet.Internal;
using Filin.Storage;

namespace Filin.EventStore.AdoNet.Tests.Internal;

sealed class AppendBlockTestDbCommandFactory : DbCommandFactory
{
    public AppendBlockTestDbCommand CommandToReturn { get; set; } = new();

    internal override DbCommand CreateAppendEventCommand(DcbEvent @event, AppendCondition appendCondition) => CommandToReturn;

    internal override DbCommand CreateAppendEventsCommand(IReadOnlyCollection<DcbEvent> events, AppendCondition appendCondition) => CommandToReturn;

    internal override DbCommand CreateReadEventsCommand(EventPosition afterPosition, DcbQuery query, int limit) => CommandToReturn;
}
