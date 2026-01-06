using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Filin.Configuration;

namespace Filin.EventStore.Postgres;

static class EventStoreFactory
{
    internal static EventStore CreateEventStore(IServiceProvider sp)
    {
        var channel = sp.GetRequiredService<Channel<Message>>();
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<EventStorePostgresOptions>>();
        return ActivatorUtilities.CreateInstance<EventStore>(sp, channel.Writer, Options.Create(optionsMonitor.CurrentValue));
    }

    internal static EventStreamingService CreateEventStreamingService(IServiceProvider sp)
    {
        var channel = sp.GetRequiredService<Channel<Message>>();
        var eventStore = sp.GetRequiredService<Storage.IEventStore>();
        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<EventStorePostgresOptions>>();
        return ActivatorUtilities.CreateInstance<EventStreamingService>(sp, channel, eventStore, Options.Create(optionsMonitor.CurrentValue));
    }
}
