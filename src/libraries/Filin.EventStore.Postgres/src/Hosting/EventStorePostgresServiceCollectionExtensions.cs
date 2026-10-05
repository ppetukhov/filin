using System.Data.Common;
using System.Threading.Channels;

using Filin.AdoNet;
using Filin.Configuration;
using Filin.EventStore.Postgres;
using Filin.EventStore.Postgres.DataFlow;
using Filin.Storage;
using Filin.Storage.DataFlow;

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
    /// Configure Postgres event storage for <see cref="IEventStore"/>.
    /// </summary>
    public static IServiceCollection AddEventStorePostgres(
        this IServiceCollection services,
        Action<OptionsBuilder<EventStorePostgresOptions>>? configureOptions = null)
    {
        configureOptions?.Invoke(services.AddOptions<EventStorePostgresOptions>());

        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<EventStorePostgresOptions>, EventStorePostgresOptionsProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<EventStorePostgresOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStorePostgresOptions>>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<EventStorePostgresOptions>, EventStorePostgresOptionsValidator>());

        var streamChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<Message> { SingleReader = true, SingleWriter = false });

        services.TryAddSingleton(streamChannel);
        services.TryAddSingleton(streamChannel.Writer);

        services.TryAddSingleton(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<EventStorePostgresOptions>>();

            var dbSource = string.IsNullOrEmpty(opt.Value.ConnectionName)
                ? DbConnectionFactory.CreateDataSource(opt.Value.Invariant, opt.Value.ConnectionString)
                : sp.GetRequiredKeyedService<DbDataSource>(opt.Value.ConnectionName);

            return ActivatorUtilities.CreateInstance<DbCommandFactory>(sp, dbSource, opt.Value.StorageSerializer);
        });

        services.TryAddSingleton<AppendEventsBlock>();
        services.TryAddFromExisting<IAppendEventsBlock, AppendEventsBlock>();

        services.TryAddSingleton<IEventStore, EventStore.Postgres.EventStore>();

        services.AddHostedService<EventStreamingService>();

        return services;
    }
}
