using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
sealed class FilinJsonStorageSerializerOptionsProvider : IPostConfigureOptions<FilinJsonStorageSerializerOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, FilinJsonStorageSerializerOptions options)
    {
        options.SerializerOptions.MakeReadOnly();
    }
}
