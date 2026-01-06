using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
sealed class EventStorePostgresOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) : IPostConfigureOptions<EventStorePostgresOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, EventStorePostgresOptions options)
    {
        foreach (var kv in typeManifestOptions.Value.EventTypes)
        {
            options.EventTypes[kv.Key] = kv.Value;
        }
    }
}
