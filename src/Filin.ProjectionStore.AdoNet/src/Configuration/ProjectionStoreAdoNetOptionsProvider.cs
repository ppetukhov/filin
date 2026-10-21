using Filin.AdoNet;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
sealed class ProjectionStoreAdoNetOptionsProvider(IServiceProvider sp, IOptions<TypeManifestOptions> typeManifestOptions) : IPostConfigureOptions<ProjectionStoreAdoNetOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, ProjectionStoreAdoNetOptions options)
    {
        foreach (var kv in typeManifestOptions.Value.ProjectionTypes)
        {
            options.ProjectionTypes[kv.Key] = kv.Value;
        }

        if (options.DataSource is null && !string.IsNullOrEmpty(options.Invariant) && AdoNetInvariants.Invariants.Contains(options.Invariant))
        {
            if (!string.IsNullOrEmpty(options.ConnectionName))
            {
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(options.ConnectionName);

                if (connectionString is not null)
                {
                    options.DataSource = DbConnectionFactory.CreateDataSource(options.Invariant, connectionString);
                }
            }
            else if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                options.DataSource = DbConnectionFactory.CreateDataSource(options.Invariant, options.ConnectionString);
            }
        }

        options.StorageSerializer ??= string.IsNullOrEmpty(options.SerializerKey)
            ? sp.GetRequiredService<IStorageSerializer>()
            : sp.GetRequiredKeyedService<IStorageSerializer>(options.SerializerKey);
    }
}
