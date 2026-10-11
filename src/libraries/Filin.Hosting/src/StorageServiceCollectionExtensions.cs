using System.Diagnostics.CodeAnalysis;

using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Filin.Hosting;

/// <summary>
/// Provides extension methods for registering storage services and providers in the application container.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "False positive for the new extension syntax.")]
public static class StorageServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        /// <summary>
        /// Registers a named projection store provider implementation in the service collection.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type of the <see cref="IProjectionStore"/>.</typeparam>
        /// <param name="name">The unique identifier name of the storage provider instance.</param>
        /// <param name="implementationFactory">The factory delegate used to construct the named storage provider instance.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        public IServiceCollection AddProjectionStore<T>(string name, Func<IServiceProvider, string, T> implementationFactory)
            where T : IProjectionStore
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(implementationFactory);

            collection
                .RemoveAllKeyed<IProjectionStore>(name)
                .TryAddKeyedSingleton<IProjectionStore>(name, (sp, key) => implementationFactory(sp, (string)key!));

            // Check if it is the default implementation
            return string.Equals(name, ProviderConstants.DefaultStorageProvider, StringComparison.OrdinalIgnoreCase)
                ? collection.Replace(ServiceDescriptor.Singleton(sp => sp.GetRequiredKeyedService<IProjectionStore>(ProviderConstants.DefaultStorageProvider)))
                : collection;
        }

        /// <summary>
        /// Registers a central event store provider implementation as a singleton service if it has not already been registered.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type of the <see cref="IEventStore"/>.</typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is <see langword="null"/>.</exception>
        public IServiceCollection AddEventStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
            where T : class, IEventStore
        {
            ArgumentNullException.ThrowIfNull(collection);

            return collection.RemoveAll<IEventStore>().AddSingleton<IEventStore, T>();
        }

        /// <summary>
        /// Registers a central event store provider implementation as a singleton service if it has not already been registered.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type of the <see cref="IEventStore"/>.</typeparam>
        /// <param name="implementationFactory">The factory delegate used to construct the event store instance.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> or <paramref name="implementationFactory"/> is <see langword="null"/>.</exception>
        public IServiceCollection AddEventStore<T>(Func<IServiceProvider, T> implementationFactory)
            where T : IEventStore
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(implementationFactory);

            return collection.RemoveAll<IEventStore>().AddSingleton<IEventStore>(sp => implementationFactory(sp));
        }

        /// <summary>
        /// Registers a dataflow propagator block capable of processing both single and batched event append requests implementation as a singleton service.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type of the <see cref="IAppendEventsBlock"/>.</typeparam>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is <see langword="null"/>.</exception>
        public IServiceCollection AddAppendEventsBlock<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
            where T : class, IAppendEventsBlock
        {
            ArgumentNullException.ThrowIfNull(collection);

            return collection.AddSingleton<IAppendEventsBlock, T>();
        }

        /// <summary>
        /// Registers a dataflow propagator block capable of processing both single and batched event append requests implementation as a singleton service.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type of the <see cref="IAppendEventsBlock"/>.</typeparam>
        /// <param name="implementationFactory">The factory delegate used to construct the event store instance.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> or <paramref name="implementationFactory"/> is <see langword="null"/>.</exception>
        public IServiceCollection AddAppendEventsBlock<T>(Func<IServiceProvider, T> implementationFactory)
            where T : IAppendEventsBlock
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(implementationFactory);

            return collection.AddSingleton<IAppendEventsBlock>(sp => implementationFactory(sp));
        }
    }
}
