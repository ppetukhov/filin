using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Extensions for <see cref="IFilinBuilder"/> instances.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "False positive for the new extension syntax.")]
public static class FilinBuilderConfigureExtensions
{
    extension(IFilinBuilder builder)
    {
        /// <summary>
        /// Adds an assembly to the builder.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The builder.</returns>
        public IFilinBuilder AddAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assembly);

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
}
