using System.Data.Common;
using System.Threading.Channels;

using Filin.AdoNet;
using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres;

static class EventStoreFactory
{
    internal static EventStore CreateEventStore(IServiceProvider sp)
    {
        var channel = sp.GetRequiredService<Channel<Message>>();
        var opt = sp.GetRequiredService<IOptions<EventStorePostgresOptions>>();

        var dbSource = string.IsNullOrEmpty(opt.Value.ConnectionName)
            ? DbConnectionFactory.CreateDataSource(opt.Value.Invariant, opt.Value.ConnectionString)
            : sp.GetRequiredKeyedService<DbDataSource>(opt.Value.ConnectionName);

        return ActivatorUtilities.CreateInstance<EventStore>(sp, dbSource, channel.Writer, opt);
    }

    internal static EventStreamingService CreateEventStreamingService(IServiceProvider sp)
    {
        var channel = sp.GetRequiredService<Channel<Message>>();
        var eventStore = sp.GetRequiredService<Storage.IEventStore>();
        var opt = sp.GetRequiredService<IOptions<EventStorePostgresOptions>>();
        return ActivatorUtilities.CreateInstance<EventStreamingService>(sp, channel, eventStore, opt);
    }
}
