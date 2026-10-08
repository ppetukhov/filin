using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Filin.Hosting;

/// <inheritdoc/>
sealed class FilinBuilder(IServiceCollection services, IConfiguration configuration) : IFilinBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services => services;

    /// <inheritdoc/>
    public IConfiguration Configuration => configuration;
}
