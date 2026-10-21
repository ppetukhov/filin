using System.Diagnostics.CodeAnalysis;

using Filin.Internal;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
sealed class FilinOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) : IConfigureOptions<FilinOptions>
{
    /// <inheritdoc/>
    public void Configure(FilinOptions options)
    {
        foreach (var kv in typeManifestOptions.Value.ProjectorTypes)
        {
            var stateType = kv.Key;
            var projectorType = kv.Value;

            var invokerType = typeof(ProjectorProxy<,>).MakeGenericType(projectorType, stateType);

            options.ProjectorProxyTypes[stateType] = invokerType;
        }

        foreach (var kv in typeManifestOptions.Value.DeciderTypes)
        {
            var (commandType, stateType) = kv.Key;
            var deciderType = kv.Value;

            var invokerType = typeof(DeciderProxy<,,>).MakeGenericType(deciderType, commandType, stateType);

            options.DeciderProxyTypes[(commandType, stateType)] = invokerType;
        }
    }
}
