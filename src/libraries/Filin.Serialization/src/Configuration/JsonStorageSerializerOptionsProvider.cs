using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
sealed class JsonStorageSerializerOptionsProvider : IPostConfigureOptions<JsonStorageSerializerOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, JsonStorageSerializerOptions options) => options.SerializerOptions.MakeReadOnly();
}
