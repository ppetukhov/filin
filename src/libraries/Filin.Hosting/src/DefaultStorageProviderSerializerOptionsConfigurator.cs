using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Provides default configuration for <see cref="IStorageProviderSerializerOptions.StorageSerializer"/>.
/// </summary>
/// <typeparam name="TOptions">The options type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultStorageProviderSerializerOptionsConfigurator{TOptions}"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider.</param>
public sealed class DefaultStorageProviderSerializerOptionsConfigurator<TOptions>(IServiceProvider serviceProvider) : IPostConfigureOptions<TOptions> where TOptions : class, IStorageProviderSerializerOptions
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // First, try to get a IStorageSerializer that was registered with 
        // the same name as the storage provider
        // If none is found, fallback to system wide default
        options.StorageSerializer ??= serviceProvider.GetKeyedService<IStorageSerializer>(name) ?? serviceProvider.GetRequiredService<IStorageSerializer>();
    }
}
