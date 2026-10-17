using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Hosting;

namespace Filin.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class FilinHostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder hostAppBuilder)
    {
        /// <summary>
        /// Configures the host app builder to host an Filin.
        /// </summary>
        /// <returns>The host builder.</returns>
        /// <remarks>
        /// Calling this method multiple times on the same <see cref="IHostApplicationBuilder"/> instance will result in one Filin being configured.
        /// </remarks>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin()")]
        public IHostApplicationBuilder UseFilin()
        {
            _ = hostAppBuilder.Services.AddFilin();

            return hostAppBuilder;
        }

        /// <summary>
        /// Configures the host builder to host an Filin.
        /// </summary>
        /// <param name="configure">The delegate used to configure the Filin.</param>
        /// <returns>The host builder.</returns>
        /// <remarks>
        /// Calling this method multiple times on the same <see cref="IHostApplicationBuilder"/> instance will result in one Filin being configured.
        /// However, the effects of <paramref name="configure"/> will be applied once for each call.
        /// </remarks>
        [RequiresUnreferencedCode("Calls Filin.Hosting.FilinServiceCollectionExtensions.extension(IServiceCollection).AddFilin(Action<IFilinBuilder>)")]
        public IHostApplicationBuilder UseFilin(Action<IFilinBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            _ = hostAppBuilder.Services.AddFilin(configure);

            return hostAppBuilder;
        }
    }
}
