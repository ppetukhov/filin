using System.Diagnostics.CodeAnalysis;

using Filin;
using Filin.Configuration;
using Filin.Hosting;
using Filin.Internal;
using Filin.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class FilinGenericHostExtensions
{
    /// <summary>
    /// Configures the host app builder to host an Filin.
    /// </summary>
    /// <param name="hostAppBuilder">The host app builder.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostApplicationBuilder"/> instance will result in one Filin being configured.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IHostApplicationBuilder UseFilin(
        this IHostApplicationBuilder hostAppBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostAppBuilder);

        return hostAppBuilder.UseFilin(_ => { });
    }

    /// <summary>
    /// Configures the host builder to host an Filin.
    /// </summary>
    /// <param name="hostAppBuilder">The host app builder.</param>
    /// <param name="configureDelegate">The delegate used to configure the Filin.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostApplicationBuilder"/> instance will result in one Filin being configured.
    /// However, the effects of <paramref name="configureDelegate"/> will be applied once for each call.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IHostApplicationBuilder UseFilin(
        this IHostApplicationBuilder hostAppBuilder,
        Action<IFilinBuilder> configureDelegate)
    {
        ArgumentNullException.ThrowIfNull(hostAppBuilder);
        ArgumentNullException.ThrowIfNull(configureDelegate);

        configureDelegate(AddFilinCore(hostAppBuilder.Services, hostAppBuilder.Configuration));

        return hostAppBuilder;
    }

    /// <summary>
    /// Configures the host builder to host an Filin.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IHostBuilder UseFilin(
        this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.UseFilin((_, __) => { });
    }

    /// <summary>
    /// Configures the host builder to host an Filin.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configureDelegate">The delegate used to configure the Filin.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
    /// However, the effects of <paramref name="configureDelegate"/> will be applied once for each call.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IHostBuilder UseFilin(
        this IHostBuilder hostBuilder,
        Action<IFilinBuilder> configureDelegate)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configureDelegate);

        return hostBuilder.UseFilin((_, builder) => configureDelegate(builder));
    }

    /// <summary>
    /// Configures the host builder to host an Filin.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configureDelegate">The delegate used to configure the Filin.</param>
    /// <returns>The host builder.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
    /// However, the effects of <paramref name="configureDelegate"/> will be applied once for each call.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IHostBuilder UseFilin(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IFilinBuilder> configureDelegate)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configureDelegate);

        return hostBuilder.ConfigureServices((context, services) => configureDelegate(context, AddFilinCore(services, context.Configuration)));
    }

    /// <summary>
    /// Configures the service collection to host an Filin.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="hostBuilderContext"></param>
    /// <param name="configureDelegate">The delegate used to configure the Filin.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// Calling this method multiple times on the same <see cref="IHostBuilder"/> instance will result in one Filin being configured.
    /// However, the effects of <paramref name="configureDelegate"/> will be applied once for each call.
    /// </remarks>
    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    public static IServiceCollection AddFilin(
        this IServiceCollection services,
        HostBuilderContext hostBuilderContext,
        Action<IFilinBuilder> configureDelegate)
    {
        ArgumentNullException.ThrowIfNull(hostBuilderContext);
        ArgumentNullException.ThrowIfNull(configureDelegate);

        var builder = AddFilinCore(services, hostBuilderContext.Configuration);

        configureDelegate(builder);

        return services;
    }

    [RequiresAssemblyFiles("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresUnreferencedCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    [RequiresDynamicCode("Calls Filin.Hosting.DefaultExecutorServices.AddDefaultServices(IFilinBuilder)")]
    static FilinBuilder AddFilinCore(IServiceCollection services, IConfiguration configuration)
    {
        var builder = new FilinBuilder(services, configuration);

        _ = builder.AddDefaultServices();

        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<FilinOptions>, FilinOptionsProvider>());
        services.TryAddSingleton(_ => new LRUCache<ProjectionId, Task<Projection?>>(10_000, TimeSpan.FromMinutes(5)));
        services.TryAddSingleton<SaveProjectionBlock>();
        services.TryAddSingleton<HandlersFactory>();
        services.TryAddSingleton<IMediator, Mediator>();

        return builder;
    }
}
