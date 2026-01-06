using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Filin.Hosting;

/// <inheritdoc/>
sealed class ExecutorBuilder(IServiceCollection services, IConfiguration configuration) : IExecutorBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services => services;

    /// <inheritdoc/>
    public IConfiguration Configuration => configuration;
}
