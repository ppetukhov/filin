using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Filin;
using Filin.Configuration;
using Filin.EventStore.Postgres;
using Filin.Hosting;
using Filin.Storage;

[assembly: ApplicationPart("Filin.EventStore.Postgres")]
[assembly: ProviderBuilder(name: "Postgres", kind: "EventStore", target: "Executor", type: typeof(EventStoreProviderBuilder))]

namespace Filin.EventStore.Postgres;

sealed class EventStoreProviderBuilder : IProviderBuilder<IExecutorBuilder>
{
    /// <inheritdoc/>
    public void Configure(IExecutorBuilder builder, string? name, IConfigurationSection configurationSection)
    {
        void OptBuilder(OptionsBuilder<EventStorePostgresOptions> optionsBuilder) => optionsBuilder.Configure<IServiceProvider>((options, services) =>
        {
            var connectionString = configurationSection[nameof(options.ConnectionString)];
            var connectionName = configurationSection["ConnectionName"];

            if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(connectionName))
            {
                connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString(connectionName);
            }

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.ConnectionString = connectionString;
            }

            var serializerKey = configurationSection["SerializerKey"];

            if (!string.IsNullOrWhiteSpace(serializerKey))
            {
                options.StorageSerializer = services.GetRequiredKeyedService<IStorageSerializer>(serializerKey);
            }
        });

        builder.Services.AddEventStorePostgres(OptBuilder);
    }
}
