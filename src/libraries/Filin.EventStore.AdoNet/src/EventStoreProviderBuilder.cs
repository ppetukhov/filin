using Filin;
using Filin.AdoNet;
using Filin.Configuration;
using Filin.EventStore.AdoNet;
using Filin.Hosting;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: ApplicationPart("Filin.EventStore.AdoNet")]
[assembly: RegisterProvider(name: "AdoNet", kind: "EventStore", type: typeof(EventStoreProviderBuilder))]

namespace Filin.EventStore.AdoNet;

sealed class EventStoreProviderBuilder : IProviderBuilder<IFilinBuilder>
{
    /// <inheritdoc/>
    public void Configure(IFilinBuilder builder, string? name, IConfigurationSection configurationSection)
    {
        var invariant = configurationSection[nameof(EventStoreAdoNetOptions.Invariant)];
        var connectionName = configurationSection[nameof(EventStoreAdoNetOptions.ConnectionName)];

        void OptBuilder(OptionsBuilder<EventStoreAdoNetOptions> optionsBuilder) => optionsBuilder.Configure<IServiceProvider>((options, services) =>
        {
            var connectionString = configurationSection[nameof(EventStoreAdoNetOptions.ConnectionString)];

            if (!string.IsNullOrWhiteSpace(connectionName))
            {
                options.ConnectionName = connectionName;
            }

            if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(connectionName))
            {
                connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString(connectionName);
            }

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.ConnectionString = connectionString;
            }

            var serializerKey = configurationSection[nameof(EventStoreAdoNetOptions.SerializerKey)];

            if (!string.IsNullOrWhiteSpace(serializerKey))
            {
                options.StorageSerializer = services.GetRequiredKeyedService<IStorageSerializer>(serializerKey);
            }
        });

        if (!string.IsNullOrEmpty(invariant) && !string.IsNullOrEmpty(connectionName))
        {
            _ = builder.Services.TryAddDbDataSource(invariant: invariant, connectionName: connectionName);
        }

        _ = builder.Services.AddEventStoreAdoNet(OptBuilder);
    }
}
