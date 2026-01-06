using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
sealed class ProjectionStoreAdoNetOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) : IPostConfigureOptions<ProjectionStoreAdoNetOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, ProjectionStoreAdoNetOptions options)
    {
        foreach (var kv in typeManifestOptions.Value.ProjectionTypes)
        {
            options.ProjectionTypes[kv.Key] = kv.Value;
        }
    }
}
