namespace Filin.Configuration;

/// <summary>
/// Defines a provider that exposes configuration logic for type manifests.
/// </summary>
public interface ITypeManifestProvider
{
    /// <summary>
    /// Configures a <see cref="TypeManifestOptions"/> instance.
    /// </summary>
    void Configure(TypeManifestOptions options);
}
