using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Hosting;

namespace Filin.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class FilinHostBuilderExtensions
{
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Configures the host builder to host an Filin.
        /// </summary>
        /// <returns>The host builder.</returns>
        /// <remarks>
        /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
        /// </remarks>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin()")]
        public IHostBuilder UseFilin() => hostBuilder.ConfigureServices((_, services) => services.AddFilin());

        /// <summary>
        /// Configures the host builder to host an Filin.
        /// </summary>
        /// <param name="configure">The delegate used to configure the Filin.</param>
        /// <returns>The host builder.</returns>
        /// <remarks>
        /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
        /// However, the effects of <paramref name="configure"/> will be applied once for each call.
        /// </remarks>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin(Action<IFilinBuilder>)")]
        public IHostBuilder UseFilin(Action<IFilinBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return hostBuilder.ConfigureServices((_, services) => services.AddFilin(configure));
        }

        /// <summary>
        /// Configures the host builder to host an Filin.
        /// </summary>
        /// <param name="configure">The delegate used to configure the Filin.</param>
        /// <returns>The host builder.</returns>
        /// <remarks>
        /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
        /// However, the effects of <paramref name="configure"/> will be applied once for each call.
        /// </remarks>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin()")]
        public IHostBuilder UseFilin(Action<HostBuilderContext, IFilinBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return hostBuilder.ConfigureServices((context, services) => configure(context, services.AddFilin()));
        }
    }
}
