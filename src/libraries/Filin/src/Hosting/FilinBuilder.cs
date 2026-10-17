using Microsoft.Extensions.DependencyInjection;

namespace Filin.Hosting;

/// <inheritdoc/>
sealed class FilinBuilder(IServiceCollection services) : IFilinBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services => services;
}
