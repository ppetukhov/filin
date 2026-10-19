using System.Diagnostics.CodeAnalysis;

using Filin.Configuration;
using Filin.Serialization;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Provides extension methods for registering serialization services within the application container.
/// </summary>
public static class SerializationServiceCollectionExtentions
{
    extension(IServiceCollection collection)
    {
        /// <summary>
        /// Registers the default storage serialization services, including options configuration and the storage serializer.
        /// </summary>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.</returns>
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize(ref Utf8JsonReader, Type, JsonSerializerOptions)")]
        public IServiceCollection AddStorageSerialization()
        {
            collection.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<JsonStorageSerializerOptions>, JsonStorageSerializerOptionsProvider>());

            collection.TryAddSingleton<IStorageSerializer, JsonStorageSerializer>();
            collection.TryAddKeyedSingleton<IStorageSerializer, JsonStorageSerializer>("json");

            return collection;
        }
    }
}
