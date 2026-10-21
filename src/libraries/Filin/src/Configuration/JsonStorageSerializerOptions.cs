using System.Text.Json;

namespace Filin.Configuration;

/// <summary>
/// Represents configuration options for the JSON-based storage serializer.
/// </summary>
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
        new JsonSerializerOptions(new JsonSerializerOptions(JsonSerializerDefaults.Strict));
#else
        new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
#endif
}
