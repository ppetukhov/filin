using Filin.Configuration;

namespace Filin.Hosting;

/// <summary>
/// Base class for generated type manifest providers.
/// </summary>
public abstract class TypeManifestProviderBase : ITypeManifestProvider
{
    /// <inheritdoc/>
    public void Configure(TypeManifestOptions options)
    {
        var key = GetType();

        if (options.TypeManifestProviders.Add(key))
        {
            ConfigureInner(options);
        }
    }

    /// <summary>
    /// Configures the provided type manifest options.
    /// </summary>
    /// <param name="options">The type manifest options.</param>
    protected abstract void ConfigureInner(TypeManifestOptions options);
}
