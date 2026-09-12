using System.Diagnostics.CodeAnalysis;

using Filin.Internal;

using Microsoft.Extensions.Options;

namespace Filin.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
sealed class ExecutorOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) : IConfigureOptions<ExecutorOptions>
{
    /// <inheritdoc/>
    public void Configure(ExecutorOptions options)
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
