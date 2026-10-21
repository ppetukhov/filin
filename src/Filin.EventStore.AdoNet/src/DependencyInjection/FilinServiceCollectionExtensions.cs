using System.Threading.Channels;

using Filin.Configuration;
using Filin.EventStore.AdoNet.DataFlow;
using Filin.EventStore.AdoNet.Internal;
using Filin.EventStore.AdoNet.Postgres;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class FilinServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        /// <summary>
        /// Configure Postgres event storage for <see cref="IEventStore"/>.
        /// </summary>
        public IServiceCollection AddEventStoreAdoNet() =>
            collection.AddEventStoreAdoNet(_ => { });

        /// <summary>
        /// Configure Postgres event storage for <see cref="IEventStore"/>.
        /// </summary>
        public IServiceCollection AddEventStoreAdoNet(
            Action<EventStoreAdoNetOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            _ = collection
                .AddOptions<EventStoreAdoNetOptions>()
                .Configure<IConfiguration>(static (options, configuration) =>
                {
                    var configurationSection = configuration.GetSection(EventStoreAdoNetOptions.SectionName);

                    var invariant = configurationSection[nameof(EventStoreAdoNetOptions.Invariant)];

                    if (!string.IsNullOrWhiteSpace(invariant))
                    {
                        options.Invariant = invariant;
                    }

                    options.ConnectionName = configurationSection[nameof(EventStoreAdoNetOptions.ConnectionName)];
                    options.ConnectionString = configurationSection[nameof(EventStoreAdoNetOptions.ConnectionString)];
                    options.SerializerKey = configurationSection[nameof(EventStoreAdoNetOptions.SerializerKey)];
                })
                .Configure(configure);

            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<EventStoreAdoNetOptions>, EventStoreAdoNetOptionsProvider>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<EventStoreAdoNetOptions>, EventStorePostgresOptionsConnectionNameValidator>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<EventStoreAdoNetOptions>, EventStorePostgresOptionsValidator>());

            var streamChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<EventStreamMessage> { SingleReader = true, SingleWriter = false });

            collection.TryAddSingleton(streamChannel);
            collection.TryAddSingleton(streamChannel.Writer);

            collection.TryAddSingleton<DbCommandFactory>(sp =>
            {
                var opt = sp.GetRequiredService<IOptions<EventStoreAdoNetOptions>>();

                return ActivatorUtilities.CreateInstance<PgDbCommandFactory>(sp, opt.Value.DataSource, opt.Value.StorageSerializer);
            });

            _ = collection.AddHostedService<EventStore.AdoNet.EventStreamingService>();

            collection.TryAddSingleton<IEventStore, EventStore.AdoNet.EventStore>();
            collection.TryAddSingleton<IAppendEventsBlock, AppendEventsBlock>();

            return collection;
        }
    }
}
