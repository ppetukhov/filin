using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
sealed class EventStoreAdoNetOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) : IPostConfigureOptions<EventStoreAdoNetOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, EventStoreAdoNetOptions options)
    {
        foreach (var kv in typeManifestOptions.Value.EventTypes)
        {
            options.EventTypes[kv.Key] = kv.Value;
        }
    }
}
