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
        /// Adds the default core and infrastructure services required for the Filin framework to function.
        /// </summary>
        /// <returns>The same <see cref="IFilinBuilder"/> instance so that multiple calls can be chained.</returns>
        [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.ApplyConfiguration(IFilinBuilder)")]
        [RequiresUnreferencedCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
        [RequiresDynamicCode("Calls Filin.Hosting.SerializationServiceCollectionExtentions.AddStorageSerialization()")]
        public IFilinBuilder AddDefaultServices()
        {
            ArgumentNullException.ThrowIfNull(builder);

            var services = builder.Services
                .AddOptions()
                .AddStorageSerialization();

            services.TryAddSingleton(TimeProvider.System);
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<TypeManifestOptions>, TypeManifestProvider>());

            return builder;
        }

        /// <summary>
        /// Adds an assembly to the builder.
        /// </summary>
        /// <param name="assemblies">The assembly.</param>
        /// <returns>The builder.</returns>
        public IFilinBuilder AddAssembly(params ReadOnlySpan<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(builder);

            foreach (var assembly in assemblies)
            {
                var attrs = assembly.GetCustomAttributes<TypeManifestProviderAttribute>();

                foreach (var attr in attrs)
                {
                    builder.Services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(ITypeManifestProvider), attr.ProviderType));
                }
            }

            return builder;
        }
    }
}
