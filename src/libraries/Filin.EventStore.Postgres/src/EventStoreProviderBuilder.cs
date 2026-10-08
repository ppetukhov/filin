using Filin;
using Filin.Configuration;
using Filin.EventStore.Postgres;
using Filin.Hosting;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: ApplicationPart("Filin.EventStore.Postgres")]
[assembly: ProviderBuilder(name: "Postgres", kind: "EventStore", target: "Executor", type: typeof(EventStoreProviderBuilder))]

namespace Filin.EventStore.Postgres;

sealed class EventStoreProviderBuilder : IProviderBuilder<IFilinBuilder>
{
    /// <inheritdoc/>
    public void Configure(IFilinBuilder builder, string? name, IConfigurationSection configurationSection)
    {
        var invariant = configurationSection[nameof(EventStorePostgresOptions.Invariant)];
        var connectionName = configurationSection[nameof(EventStorePostgresOptions.ConnectionName)];

        void OptBuilder(OptionsBuilder<EventStorePostgresOptions> optionsBuilder) => optionsBuilder.Configure<IServiceProvider>((options, services) =>
        {
            var connectionString = configurationSection[nameof(EventStorePostgresOptions.ConnectionString)];

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

            var serializerKey = configurationSection[nameof(EventStorePostgresOptions.SerializerKey)];

            if (!string.IsNullOrWhiteSpace(serializerKey))
            {
                options.StorageSerializer = services.GetRequiredKeyedService<IStorageSerializer>(serializerKey);
            }
        });

        if (!string.IsNullOrEmpty(invariant) && !string.IsNullOrEmpty(connectionName))
        {
            _ = builder.Services.TryAddDbDataSource(invariant: invariant, connectionName: connectionName);
        }

        _ = builder.Services.AddEventStorePostgres(OptBuilder);
    }
}
