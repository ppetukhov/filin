using Microsoft.Extensions.DependencyInjection;

namespace Filin.Configuration;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
static class FromExistingServiceCollectionExtensions
{
    /// <summary>
    /// Registers an existing registration of <typeparamref name="TImplementation"/> as a provider of service type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service type being provided.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    internal static void AddFromExisting<TService, TImplementation>(this IServiceCollection services) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFromExisting(typeof(TService), typeof(TImplementation));
    }

    internal static void AddFromExisting<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFromExisting(typeof(TService), typeof(TImplementation), serviceKey);
    }

    /// <summary>
    /// Registers an existing registration of <paramref name="implementation"/> as a provider of service type <paramref name="service"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="service">The service type being provided.</param>
    /// <param name="implementation">The implementation of <paramref name="service"/>.</param>
    /// <param name="serviceKey"></param>
    internal static void AddFromExisting(this IServiceCollection services, Type service, Type implementation, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(implementation);

        ServiceDescriptor? registration = null;

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == implementation && descriptor.ServiceKey == serviceKey)
            {
                registration = descriptor;
                break;
            }
        }

        if (registration is null)
        {
            var str = serviceKey is not null ? $" with service serviceKey '{serviceKey}'" : null;

            throw new ArgumentNullException(nameof(implementation), $"Unable to find previously registered ServiceType of '{implementation.FullName}'{str}");
        }

        var newRegistration = serviceKey is null
            ? new ServiceDescriptor(service, sp => sp.GetRequiredService(implementation), registration.Lifetime)
            : new ServiceDescriptor(service, serviceKey, (sp, key) => sp.GetRequiredKeyedService(implementation, key), registration.Lifetime);

        services.Add(newRegistration);
    }

    /// <summary>
    /// Registers an existing registration of <typeparamref name="TImplementation"/> as a provider of <typeparamref name="TService"/> if there are no existing <typeparamref name="TService"/> implementations.
    /// </summary>
    /// <typeparam name="TService">The service type being provided.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    internal static void TryAddFromExisting<TService, TImplementation>(this IServiceCollection services) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.All(service => !service.IsKeyedService && service.ServiceType != typeof(TService)))
        {
            services.AddFromExisting<TService, TImplementation>();
        }
    }

    internal static void TryAddFromExisting<TService, TImplementation>(this IServiceCollection services, object? serviceKey) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.All(service => service.ServiceType != typeof(TService) && service.ServiceKey != serviceKey))
        {
            services.AddFromExisting<TService, TImplementation>(serviceKey);
        }
    }
}
