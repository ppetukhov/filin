using System.Reflection;

using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Extensions for <see cref="IFilinBuilder"/> instances.
/// </summary>
public static class ExecutorBuilderConfigureExtensions
{
    /// <summary>
    /// Adds an assembly to the builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The builder.</returns>
    public static IFilinBuilder AddAssembly(this IFilinBuilder builder, Assembly assembly)
    {
        var attrs = assembly.GetCustomAttributes<TypeManifestProviderAttribute>();

        foreach (var attr in attrs)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(ITypeManifestProvider), attr.ProviderType));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());

        return builder;
    }
}
