using Filin;
using Filin.Configuration;
using Filin.Hosting;
using Filin.ProjectionStore.AdoNet;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: ApplicationPart("Filin.ProjectionStore.AdoNet")]
[assembly: ProviderBuilder("AdoNet", "ProjectionStore", "Executor", typeof(ProjectionStoreProviderBuilder))]

namespace Filin.ProjectionStore.AdoNet;

sealed class ProjectionStoreProviderBuilder : IProviderBuilder<IFilinBuilder>
{
    /// <inheritdoc/>
    public void Configure(IFilinBuilder builder, string? name, IConfigurationSection configurationSection)
    {
        var invariant = configurationSection[nameof(ProjectionStoreAdoNetOptions.Invariant)];
        var connectionName = configurationSection[nameof(ProjectionStoreAdoNetOptions.ConnectionName)];

        void OptBuilder(OptionsBuilder<ProjectionStoreAdoNetOptions> optionsBuilder) => optionsBuilder.Configure<IServiceProvider>((options, services) =>
        {
            var connectionString = configurationSection[nameof(options.ConnectionString)];

            if (!string.IsNullOrEmpty(connectionName))
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

            var serializerKey = configurationSection[nameof(ProjectionStoreAdoNetOptions.SerializerKey)];

            if (!string.IsNullOrWhiteSpace(serializerKey))
            {
                options.StorageSerializer = services.GetRequiredKeyedService<IStorageSerializer>(serializerKey);
            }
        });

        if (string.IsNullOrWhiteSpace(name))
        {
            name = ProviderConstants.DefaultStorageProvider;
        }

        if (!string.IsNullOrEmpty(invariant) && !string.IsNullOrEmpty(connectionName))
        {
            builder.Services.TryAddDbDataSource(invariant: invariant, connectionName: connectionName);
        }

        builder.Services.AddProjectionStoreAdoNet(name, OptBuilder);
    }
}
