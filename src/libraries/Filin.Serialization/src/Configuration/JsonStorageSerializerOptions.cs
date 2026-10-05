using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Filin.Configuration;

/// <summary>
/// Represents configuration options for the JSON-based storage serializer.
/// </summary>
/// <remarks>
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
public sealed class JsonStorageSerializerOptions
{
    /// <summary>
    /// Gets the underlying reflection-based configuration options for <see cref="JsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="JsonSerializerOptions" path="/summary"/>
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; } =
#if NET9_0_OR_GREATER
        new JsonSerializerOptions(JsonSerializerOptions.Strict);
#else
        new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
#endif
}
