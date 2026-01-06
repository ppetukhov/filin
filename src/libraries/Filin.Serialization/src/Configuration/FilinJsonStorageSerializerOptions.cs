using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Filin.Configuration;

[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
sealed class FilinJsonStorageSerializerOptions
{
    /// <inheritdoc cref="JsonSerializerOptions"/>
    public required JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions(JsonSerializerOptions.Strict);
}
