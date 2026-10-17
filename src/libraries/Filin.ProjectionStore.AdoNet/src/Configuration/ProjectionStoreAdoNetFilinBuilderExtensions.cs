using Filin.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class ProjectionStoreAdoNetFilinBuilderExtensions
{
    extension(IFilinBuilder builder)
    {
        /// <summary>
        /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
        /// </summary>
        public IFilinBuilder AddProjectionStoreAdoNet() =>
            builder.AddProjectionStoreAdoNet(_ => { });

        /// <summary>
        /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
        /// </summary>
        public IFilinBuilder AddProjectionStoreAdoNet(Action<ProjectionStoreAdoNetOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var collection = builder.Services;

            _ = collection
                .AddOptions<ProjectionStoreAdoNetOptions>()
                .Configure<IConfiguration>(static (options, configuration) =>
                {
                    var configurationSection = configuration.GetSection(ProjectionStoreAdoNetOptions.SectionName);

                    var invariant = configurationSection[nameof(ProjectionStoreAdoNetOptions.Invariant)];

                    if (!string.IsNullOrWhiteSpace(invariant))
                    {
                        options.Invariant = invariant;
                    }

                    options.ConnectionName = configurationSection[nameof(ProjectionStoreAdoNetOptions.ConnectionName)];
                    options.ConnectionString = configurationSection[nameof(ProjectionStoreAdoNetOptions.ConnectionString)];
                    options.SerializerKey = configurationSection[nameof(ProjectionStoreAdoNetOptions.SerializerKey)];
                })
                .Configure(configure);

            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsProvider>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, DefaultStorageProviderSerializerOptionsConfigurator<ProjectionStoreAdoNetOptions>>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsConnectionNameValidator>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsValidator>());

            _ = collection.AddProjectionStore<ProjectionStore.AdoNet.ProjectionStore>();

            return builder;
        }
    }
}
