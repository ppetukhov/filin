using System.Threading.Channels;

using Filin.Configuration;
using Filin.DataFlow;
using Filin.EventStore.Postgres;
using Filin.EventStore.Postgres.DataFlow;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class EventStorePostgresServiceCollectionExtensions
{
    /// <summary>
    /// Configure AdoNet event storage for <see cref="IEventStore"/>.
    /// </summary>
    public static IServiceCollection AddEventStorePostgres(
        this IServiceCollection services,
        Action<OptionsBuilder<EventStorePostgresOptions>>? configureOptions = null)
    {
        var opt = services.AddOptions<EventStorePostgresOptions>();

        configureOptions?.Invoke(services.AddOptions<EventStorePostgresOptions>());
        services.ConfigureOptionsForLogging<EventStorePostgresOptions>();
        services.AddTransient<IPostConfigureOptions<EventStorePostgresOptions>, EventStorePostgresOptionsProvider>();
        services.AddTransient<IPostConfigureOptions<EventStorePostgresOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStorePostgresOptions>>();
        services.AddSingleton<IValidateOptions<EventStorePostgresOptions>, EventStorePostgresOptionsValidator>();

        if (services.Contains(MarkerSreamingService.ServiceDescriptor))
        {
            return services;
        }

        services.Add(MarkerSreamingService.ServiceDescriptor);

        var streamChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<Message> { SingleReader = true, SingleWriter = false });

        services.TryAddSingleton(streamChannel);

        services.TryAddSingleton(EventStoreFactory.CreateAppendEventsBlock);

        services.TryAddFromExisting<IAppendEventsBlock, AppendEventsBlock>();

        services.AddEventStore(EventStoreFactory.CreateEventStore);

        services.AddHostedService(EventStoreFactory.CreateEventStreamingService);

        return services;
    }
    sealed class MarkerSreamingService
    {
        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton(new MarkerSreamingService());
    }
}
