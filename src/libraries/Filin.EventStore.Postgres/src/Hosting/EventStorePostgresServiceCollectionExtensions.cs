using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Filin.Configuration;
using Filin.EventStore.Postgres;
using Filin.Storage;

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
        configureOptions?.Invoke(services.AddOptions<EventStorePostgresOptions>());
        services.ConfigureOptionsForLogging<EventStorePostgresOptions>();
        services.AddTransient<IPostConfigureOptions<EventStorePostgresOptions>, EventStorePostgresOptionsProvider>();
        services.AddTransient<IPostConfigureOptions<EventStorePostgresOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStorePostgresOptions>>();
        services.AddSingleton<IValidateOptions<EventStorePostgresOptions>, EventStorePostgresOptionsValidator>();

        if (!services.Any(d => d.ServiceType.Equals(typeof(MarkerSreamingService))
                               && d.ImplementationInstance is MarkerSreamingService marker))
        {
            var streamChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<Message> { SingleReader = true, SingleWriter = false });

            services.TryAddSingleton(streamChannel);

            services.AddEventStore(EventStoreFactory.CreateEventStore);

            services.AddHostedService(EventStoreFactory.CreateEventStreamingService);

            services.AddSingleton(new MarkerSreamingService());
        }

        return services;
    }

    sealed record MarkerSreamingService();
}
