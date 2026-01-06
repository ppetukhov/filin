using Filin.Configuration;

namespace Filin.Hosting;

public interface ITypeManifestProvider
{
    /// <summary>
    /// Configures a <see cref="TypeManifestOptions"/> instance.
    /// </summary>
    void Configure(TypeManifestOptions options);
}
