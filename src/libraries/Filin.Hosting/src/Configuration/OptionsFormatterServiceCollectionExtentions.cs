using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

namespace Filin.Configuration;

/// <summary>
/// Extension methods on <see cref="IServiceCollection"/>, to provider better usability to IOptionFormatter.
/// </summary>
public static class OptionsFormatterServiceCollectionExtentions
{
    /// <summary>
    /// Configures an options formatter for <typeparamref name="TOptions"/>.
    /// </summary>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <typeparam name="TOptions">
    /// The options type.
    /// </typeparam>
    /// <typeparam name="TOptionFormatter">
    /// The option formatter type.
    /// </typeparam>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection ConfigureFormatter<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionFormatter>(this IServiceCollection services)
        where TOptions : class
        where TOptionFormatter : class, IOptionsFormatter<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.All(service => service.ServiceType != typeof(IOptionsFormatter<TOptions>)))
        {
            services
                .AddSingleton<IOptionsFormatter<TOptions>, TOptionFormatter>()
                .AddFromExisting<IOptionsFormatter, IOptionsFormatter<TOptions>>();
        }
        else
        {
            // override IOptionFormatter<TOptions>
            _ = services.AddSingleton<IOptionsFormatter<TOptions>, TOptionFormatter>();
        }
        return services;
    }

    /// <summary>
    /// Configures an options formatter for <typeparamref name="TOptions"/>.
    /// </summary>
    /// <remarks>
    /// This will use the default options formatter unless a non-default formatter is configured.
    /// </remarks>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <typeparam name="TOptions">
    /// The options type.
    /// </typeparam>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection ConfigureFormatter<TOptions>(this IServiceCollection services)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<IOptionsFormatter>(sp => sp.GetRequiredService<IOptionsFormatter<TOptions>>());
    }

    /// <summary>
    /// Configures an options formatter for <typeparamref name="TOptions"/> if none are already configured.
    /// </summary>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <typeparam name="TOptions">
    /// The options type.
    /// </typeparam>
    /// <typeparam name="TOptionFormatter">
    /// The option formatter type.
    /// </typeparam>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection TryConfigureFormatter<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionFormatter>(this IServiceCollection services)
        where TOptions : class
        where TOptionFormatter : class, IOptionsFormatter<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.All(service => service.ServiceType != typeof(IOptionsFormatter<TOptions>)))
        {
            _ = services.ConfigureFormatter<TOptions, TOptionFormatter>();
        }

        return services;
    }

    /// <summary>
    /// Configures an options formatter resolver for <typeparamref name="TOptions"/>.
    /// </summary>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <typeparam name="TOptions">
    /// The options type.
    /// </typeparam>
    /// <typeparam name="TOptionFormatterResolver">
    /// The option formatter resolver type.
    /// </typeparam>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection ConfigureFormatterResolver<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionFormatterResolver>(this IServiceCollection services)
        where TOptions : class
        where TOptionFormatterResolver : class, IOptionsFormatterResolver<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<IOptionsFormatterResolver<TOptions>, TOptionFormatterResolver>();
    }

    /// <summary>
    /// Configure option formatter resolver for named option TOptions, if none is configured
    /// </summary>
    public static IServiceCollection TryConfigureFormatterResolver<TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOptionFormatterResolver>(this IServiceCollection services)
        where TOptions : class
        where TOptionFormatterResolver : class, IOptionsFormatterResolver<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.All(service => service.ServiceType != typeof(IOptionsFormatterResolver<TOptions>))
            ? services.ConfigureFormatterResolver<TOptions, TOptionFormatterResolver>()
            : services;
    }

    /// <summary>
    /// Configures a named option to be logged.
    /// </summary>
    /// <typeparam name="TOptions">
    /// The option object's type.
    /// </typeparam>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection ConfigureOptionsForLogging<TOptions>(this IServiceCollection services)
        where TOptions : class => services.AddSingleton<IOptionsFormatter>(sp => sp.GetRequiredService<IOptionsFormatterResolver<TOptions>>().Resolve(null));

    /// <summary>
    /// Configures a named option to be logged.
    /// </summary>
    /// <typeparam name="TOptions">
    /// The option object's type.
    /// </typeparam>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <param name="name">
    /// The option object's name.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/>, for chaining with other calls.
    /// </returns>
    public static IServiceCollection ConfigureOptionsForLogging<TOptions>(this IServiceCollection services, string name)
        where TOptions : class
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return services.AddSingleton<IOptionsFormatter>(sp => sp.GetRequiredService<IOptionsFormatterResolver<TOptions>>().Resolve(name));
    }
}
