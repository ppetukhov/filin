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
    /// <summary>
    /// Configure AdoNet storage as the default <see cref="Storage.IProjectionStore"/>.
    /// </summary>
    public static IServiceCollection AddProjectionStoreAdoNet(this IServiceCollection services, Action<ProjectionStoreAdoNetOptions> configureOptions) =>
        services.AddProjectionStoreAdoNet(ProviderConstants.DefaultStorageProvider, ob => ob.Configure(configureOptions));

    /// <summary>
    /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
    /// </summary>
    public static IServiceCollection AddProjectionStoreAdoNet(this IServiceCollection services, string name, Action<ProjectionStoreAdoNetOptions> configureOptions) =>
        services.AddProjectionStoreAdoNet(name, ob => ob.Configure(configureOptions));

    /// <summary>
    /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
    /// </summary>
    public static IServiceCollection AddProjectionStoreAdoNet(this IServiceCollection services, Action<OptionsBuilder<ProjectionStoreAdoNetOptions>>? configureOptions = null) =>
        services.AddProjectionStoreAdoNet(ProviderConstants.DefaultStorageProvider, configureOptions);

    /// <summary>
    /// Configure AdoNet storage for <see cref="Storage.IProjectionStore"/>.
    /// </summary>
    public static IServiceCollection AddProjectionStoreAdoNet(
        this IServiceCollection services,
        string name,
        Action<OptionsBuilder<ProjectionStoreAdoNetOptions>>? configureOptions = null)
    {
        configureOptions?.Invoke(services.AddOptions<ProjectionStoreAdoNetOptions>(name));
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<ProjectionStoreAdoNetOptions>, DefaultStorageProviderSerializerOptionsConfigurator<ProjectionStoreAdoNetOptions>>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IValidateOptions<ProjectionStoreAdoNetOptions>, ProjectionStoreAdoNetOptionsValidator>());
        return services.AddProjectionStore(name, ProjectionStoreFactory.Create);
    }
}
