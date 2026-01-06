using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Filin;
using Filin.Configuration;
using Filin.ProjectionStore.AdoNet;
using Filin.Hosting;
using Filin.Storage;

[assembly: ApplicationPart("Filin.ProjectionStore.AdoNet")]
[assembly: ProviderBuilder("AdoNet", "ProjectionStore", "Executor", typeof(ProjectionStoreProviderBuilder))]

namespace Filin.ProjectionStore.AdoNet;

sealed class ProjectionStoreProviderBuilder : IProviderBuilder<IExecutorBuilder>
{
    /// <inheritdoc/>
    public void Configure(IExecutorBuilder builder, string? name, IConfigurationSection configurationSection)
    {
        void OptBuilder(OptionsBuilder<ProjectionStoreAdoNetOptions> optionsBuilder) => optionsBuilder.Configure<IServiceProvider>((options, services) =>
        {
            var invariant = configurationSection[nameof(options.Invariant)];

            if (!string.IsNullOrWhiteSpace(invariant))
            {
                options.Invariant = invariant;
            }

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

        if (string.IsNullOrWhiteSpace(name))
        {
            name = ProviderConstants.DefaultStorageProvider;
        }

        builder.Services.AddProjectionStoreAdoNet(name, OptBuilder);
    }
}
