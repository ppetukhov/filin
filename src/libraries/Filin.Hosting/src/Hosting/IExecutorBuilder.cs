using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Filin.Hosting;

/// <summary>
/// Builder for configuring an Filin server.
/// </summary>
public interface IExecutorBuilder
{
    /// <summary>
    /// The services shared by the Filin and host.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    IConfiguration Configuration { get; }
}
