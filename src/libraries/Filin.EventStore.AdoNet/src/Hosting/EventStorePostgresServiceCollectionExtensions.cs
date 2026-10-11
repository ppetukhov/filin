using System.Data.Common;
using System.Threading.Channels;

using Filin.AdoNet;
using Filin.Configuration;
using Filin.EventStore.AdoNet;
using Filin.EventStore.AdoNet.DataFlow;
using Filin.EventStore.AdoNet.Internal;
using Filin.EventStore.AdoNet.Postgres;
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
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configure Postgres event storage for <see cref="IEventStore"/>.
        /// </summary>
        public IServiceCollection AddEventStoreAdoNet(
            Action<OptionsBuilder<EventStoreAdoNetOptions>>? configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<EventStoreAdoNetOptions>());

            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<EventStoreAdoNetOptions>, EventStoreAdoNetOptionsProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<EventStoreAdoNetOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStoreAdoNetOptions>>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<EventStoreAdoNetOptions>, EventStorePostgresOptionsValidator>());

            var streamChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<EventStreamMessage> { SingleReader = true, SingleWriter = false });

            services.TryAddSingleton(streamChannel);
            services.TryAddSingleton(streamChannel.Writer);

            services.TryAddSingleton<DbCommandFactory>(sp =>
            {
                var opt = sp.GetRequiredService<IOptions<EventStoreAdoNetOptions>>();

                var dbSource = string.IsNullOrEmpty(opt.Value.ConnectionName)
                    ? DbConnectionFactory.CreateDataSource(opt.Value.Invariant, opt.Value.ConnectionString)
                    : sp.GetRequiredKeyedService<DbDataSource>(opt.Value.ConnectionName);

                return ActivatorUtilities.CreateInstance<PgDbCommandFactory>(sp, dbSource, opt.Value.StorageSerializer);
            });

            return services
                .AddAppendEventsBlock<AppendEventsBlock>()
                .AddEventStore<EventStore.AdoNet.EventStore>()
                .AddHostedService<EventStreamingService>();
        }
    }
}
