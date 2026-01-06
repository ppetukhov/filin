using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Filin.Configuration;

namespace Filin.ProjectionStore.AdoNet;

static class ProjectionStoreFactory
{
    internal static ProjectionStore Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<ProjectionStoreAdoNetOptions>>();
        return ActivatorUtilities.CreateInstance<ProjectionStore>(services, Options.Create(optionsMonitor.Get(name)), name);
    }
}
