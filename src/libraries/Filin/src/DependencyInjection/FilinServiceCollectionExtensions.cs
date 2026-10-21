using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Configuration;
using Filin.Internal;
using Filin.Serialization;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class FilinServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        /// <summary>
        /// Configures the service collection to host an Filin.
        /// </summary>
        /// <returns>The service collection.</returns>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize(ref Utf8JsonReader, Type, JsonSerializerOptions)")]
        public IServiceCollection AddFilin()
        {
            _ = collection.AddOptions();

            collection.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<FilinOptions>, FilinOptionsProvider>());
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<JsonStorageSerializerOptions>, JsonStorageSerializerOptionsProvider>());

            collection.TryAddSingleton<IStorageSerializer, JsonStorageSerializer>();
            collection.TryAddKeyedSingleton<IStorageSerializer, JsonStorageSerializer>("json");

            collection.TryAddSingleton(_ => new LRUCache<ProjectionId, Task<Projection?>>(10_000, TimeSpan.FromMinutes(5)));
            collection.TryAddSingleton<SaveProjectionBlock>();
            collection.TryAddSingleton<HandlersFactory>();
            collection.TryAddSingleton<IMediator, Mediator>();

            return collection;
        }

        /// <summary>
        /// Adds an domain assembly with <see cref="ITypeManifestProvider"/>.
        /// </summary>
        /// <param name="assemblies">The assembly.</param>
        /// <returns>The builder.</returns>
        public IServiceCollection AddTypeManifestAssembly(params ReadOnlySpan<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var attrs = assembly.GetCustomAttributes<TypeManifestProviderAttribute>();

                foreach (var attr in attrs)
                {
                    collection.TryAddEnumerable(ServiceDescriptor.Transient(typeof(ITypeManifestProvider), attr.ProviderType));
                }
            }

            return collection;
        }
    }
}
