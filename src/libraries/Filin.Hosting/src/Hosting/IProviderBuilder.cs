using Microsoft.Extensions.Configuration;

namespace Filin.Hosting;

/// <summary>
/// Configures the provider.
/// </summary>
/// <typeparam name="TBuilder">Type of provider builder.</typeparam>
public interface IProviderBuilder<TBuilder>
{
    /// <summary>
    /// Configures the provider.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The provider name, or <see langword="null"/> if no name is specified.</param>
    /// <param name="configurationSection">The configuration section containing provider configuration.</param>
    void Configure(TBuilder builder, string? name, IConfigurationSection configurationSection);
}
