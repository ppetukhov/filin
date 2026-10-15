using Filin.Configuration;
using Filin.ProjectionStore.AdoNet;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class ProjectionStoreAdoNetServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configure AdoNet storage as the default <see cref="Storage.IProjectionStore"/>.
        /// </summary>
        public IServiceCollection AddProjectionStoreAdoNet(Action<ProjectionStoreAdoNetOptions> configureOptions) =>
            services.AddProjectionStoreAdoNet(ob => ob.Configure(configureOptions));

        /// <summary>
        /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
        /// </summary>
        public IServiceCollection AddProjectionStoreAdoNet(Action<OptionsBuilder<ProjectionStoreAdoNetOptions>>? configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<ProjectionStoreAdoNetOptions>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, DefaultStorageProviderSerializerOptionsConfigurator<ProjectionStoreAdoNetOptions>>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsValidator>());
            return services.AddProjectionStore(ProjectionStoreFactory.Create);
        }
    }
}
