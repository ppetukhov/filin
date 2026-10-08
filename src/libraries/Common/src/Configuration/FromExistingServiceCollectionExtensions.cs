using Microsoft.Extensions.DependencyInjection;

namespace Filin.Configuration;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
static class FromExistingServiceCollectionExtensions
{
    /// <summary>
    /// Add an existing registration of <typeparamref name="TImplementation"/> as a provider of service type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service type being provided.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="collection">The service collection.</param>
    /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddFromExisting<TService, TImplementation>(this IServiceCollection collection) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(collection);

        return collection.AddFromExisting(typeof(TService), typeof(TImplementation));
    }

    /// <summary>
    /// Add an existing registration of <typeparamref name="TImplementation"/> as a provider of service type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service type being provided.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="collection">The service collection.</param>
    /// <param name="serviceKey"></param>
    /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddFromExisting<TService, TImplementation>(this IServiceCollection collection, object? serviceKey) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(collection);

        return collection.AddFromExisting(typeof(TService), typeof(TImplementation), serviceKey);
    }

    /// <summary>
    /// Add an existing registration of <paramref name="implementation"/> as a provider of service type <paramref name="service"/>.
    /// </summary>
    /// <param name="collection">The service collection.</param>
    /// <param name="service">The service type being provided.</param>
    /// <param name="implementation">The implementation of <paramref name="service"/>.</param>
    /// <param name="serviceKey"></param>
    /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddFromExisting(this IServiceCollection collection, Type service, Type implementation, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(implementation);

        ServiceDescriptor? registration = null;

        foreach (var descriptor in collection)
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

        collection.Add(newRegistration);

        return collection;
    }

    /// <summary>
    /// Add an existing registration of <typeparamref name="TImplementation"/> as a provider of <typeparamref name="TService"/> to the <paramref name="collection"/> if there are no existing <typeparamref name="TService"/> implementations.
    /// </summary>
    /// <typeparam name="TService">The service type being provided.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="collection">The service collection.</param>
    /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection TryAddFromExisting<TService, TImplementation>(this IServiceCollection collection) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(collection);

        return collection.All(service => !service.IsKeyedService && service.ServiceType != typeof(TService))
            ? collection.AddFromExisting<TService, TImplementation>()
            : collection;
    }

    /// <summary>
    /// Add an existing registration of <typeparamref name="TImplementation"/> as a provider of <typeparamref name="TService"/> to the <paramref name="collection"/> if there are no existing <typeparamref name="TService"/> implementations.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
    /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey"></param>
    /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection TryAddFromExisting<TService, TImplementation>(this IServiceCollection collection, object? serviceKey) where TImplementation : TService
    {
        ArgumentNullException.ThrowIfNull(collection);

        return collection.All(service => service.ServiceType != typeof(TService) && service.ServiceKey != serviceKey)
            ? collection.AddFromExisting<TService, TImplementation>(serviceKey)
            : collection;
    }
}
