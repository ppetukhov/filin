using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Filin.Hosting;

public static class StorageServiceCollectionExtentions
{
    /// <summary>
    /// Add a storage provider implementation to the Filin.
    /// </summary>
    /// <typeparam name="T">The concrete implementation type of the grain storage provider.</typeparam>
    /// <param name="collection">The service collection.</param>
    /// <param name="name">The name of the storage to add.</param>
    /// <param name="implementationFactory">Factory to build the storage provider.</param>
    /// <returns>The service provider.</returns>
    public static IServiceCollection AddProjectionStore<T>(this IServiceCollection collection, string name, Func<IServiceProvider, string, T> implementationFactory)
        where T : IProjectionStore
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        collection.TryAddKeyedSingleton<IProjectionStore>(name, (sp, key) => implementationFactory(sp, (string)key!));

        // Check if it is the default implementation
        if (string.Equals(name, ProviderConstants.DefaultStorageProvider, StringComparison.OrdinalIgnoreCase))
        {
            collection.TryAddSingleton(sp => sp.GetRequiredKeyedService<IProjectionStore>(ProviderConstants.DefaultStorageProvider));
        }

        return collection;
    }

    /// <summary>
    /// Add a storage provider implementation to the Filin.
    /// </summary>
    /// <typeparam name="T">The concrete implementation type of the grain storage provider.</typeparam>
    /// <param name="collection">The service collection.</param>
    /// <param name="implementationFactory">Factory to build the storage provider.</param>
    /// <returns>The service provider.</returns>
    public static IServiceCollection AddEventStore<T>(this IServiceCollection collection, Func<IServiceProvider, T> implementationFactory)
        where T : IEventStore
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        collection.TryAddSingleton<IEventStore>((sp) => implementationFactory(sp));

        return collection;
    }
}
