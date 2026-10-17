using System.Diagnostics.CodeAnalysis;

using Filin.Configuration;
using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class FilinServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures the service collection to host an Filin.
        /// </summary>
        /// <returns>The service collection.</returns>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinBuilderConfigureExtensions.extension(IFilinBuilder).AddDefaultServices()")]
        public IFilinBuilder AddFilin()
        {
            var builder = new FilinBuilder(services);

            _ = builder.AddDefaultServices();

            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<FilinOptions>, FilinOptionsProvider>());
            services.TryAddSingleton(_ => new LRUCache<ProjectionId, Task<Projection?>>(10_000, TimeSpan.FromMinutes(5)));
            services.TryAddSingleton<SaveProjectionBlock>();
            services.TryAddSingleton<HandlersFactory>();
            services.TryAddSingleton<IMediator, Mediator>();

            return builder;
        }

        /// <summary>
        /// Configures the service collection to host an Filin.
        /// </summary>
        /// <param name="configure">The delegate used to configure the Filin.</param>
        /// <returns>The service collection.</returns>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin()")]
        public IFilinBuilder AddFilin(Action<IFilinBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var builder = services.AddFilin();

            configure(builder);

            return builder;
        }
    }
}
