using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Filin.Hosting;

/// <summary>
/// Provides extension methods for registering storage services and providers in the application container.
/// </summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers a named projection store provider implementation in the service collection.
    /// </summary>
    /// <typeparam name="T">The concrete implementation type of the <see cref="IProjectionStore"/>.</typeparam>
    /// <param name="collection">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The unique identifier name of the storage provider instance.</param>
    /// <param name="implementationFactory">The factory delegate used to construct the named storage provider instance.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
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
    /// Registers a central event store provider implementation as a singleton service if it has not already been registered.
    /// </summary>
    /// <typeparam name="T">The concrete implementation type of the <see cref="IEventStore"/>.</typeparam>
    /// <param name="collection">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="implementationFactory">The factory delegate used to construct the event store instance.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> or <paramref name="implementationFactory"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddEventStore<T>(this IServiceCollection collection, Func<IServiceProvider, T> implementationFactory)
        where T : IEventStore
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(implementationFactory);

        collection.TryAddSingleton<IEventStore>((sp) => implementationFactory(sp));

        return collection;
    }
}
