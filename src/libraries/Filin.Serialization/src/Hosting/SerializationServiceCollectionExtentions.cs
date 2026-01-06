using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Filin.Configuration;
using Filin.Serialization;
using Filin.Storage;

namespace Filin.Hosting;

public static class SerializationServiceCollectionExtentions
{
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize(ref Utf8JsonReader, Type, JsonSerializerOptions)")]
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    public static IServiceCollection AddStorageSerialization(this IServiceCollection services)
    {
        services.TryAddSingleton<IPostConfigureOptions<FilinJsonStorageSerializerOptions>, FilinJsonStorageSerializerOptionsProvider>();

        return services.AddSingleton<IStorageSerializer, FilinJsonStorageSerializer>();
    }
}
