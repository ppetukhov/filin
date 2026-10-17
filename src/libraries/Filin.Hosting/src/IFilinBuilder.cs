using Microsoft.Extensions.DependencyInjection;

namespace Filin.Hosting;

/// <summary>
/// Builder for configuring an Filin.
/// </summary>
public interface IFilinBuilder
{
    /// <summary>
    /// The services shared by the Filin and host.
    /// </summary>
    IServiceCollection Services { get; }
}
