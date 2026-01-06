using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Filin.Configuration;

namespace Filin.Hosting;

/// <summary>
/// Extensions for <see cref="IExecutorBuilder"/> instances.
/// </summary>
public static class ExecutorBuilderConfigureExtensions
{
    /// <summary>
    /// Adds an assembly to the builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The builder.</returns>
    public static IExecutorBuilder AddAssembly(this IExecutorBuilder builder, Assembly assembly)
    {
        var attrs = assembly.GetCustomAttributes<TypeManifestProviderAttribute>();

        foreach (var attr in attrs)
        {
            _ = builder.Services.AddSingleton(typeof(ITypeManifestProvider), attr.ProviderType);
        }

        builder.Services.TryAddSingleton<IConfigureOptions<TypeManifestOptions>, TypeManifestProvider>();
        builder.Services.TryAddSingleton<IPostConfigureOptions<TypeManifestOptions>, TypeManifestProvider>();

        return builder;
    }
}
