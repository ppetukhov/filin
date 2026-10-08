using Filin.Configuration;

using Microsoft.Extensions.Options;

namespace Filin.Hosting;

/// <summary>
/// Configure options by inner manifest providers.
/// </summary>
sealed class TypeManifestProvider(IEnumerable<ITypeManifestProvider> providers) : IConfigureOptions<TypeManifestOptions>, IPostConfigureOptions<TypeManifestOptions>
{
    /// <inheritdoc/>
    public void Configure(TypeManifestOptions options)
    {
        var completed = new HashSet<Type>();

        foreach (var provider in providers)
        {
            if (completed.Add(provider.GetType()))
            {
                provider.Configure(options);
            }
        }
    }

    /// <inheritdoc/>
    public void PostConfigure(string? name, TypeManifestOptions options) => options.TypeManifestProviders.Clear();
}
