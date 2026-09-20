using System.Data.Common;

using Filin.AdoNet;
using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.ProjectionStore.AdoNet;

static class ProjectionStoreFactory
{
    internal static ProjectionStore Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<ProjectionStoreAdoNetOptions>>();

        var opt = optionsMonitor.Get(name);

        var dbSource = string.IsNullOrEmpty(opt.ConnectionName)
            ? DbConnectionFactory.CreateDataSource(opt.Invariant, opt.ConnectionString)
            : services.GetRequiredKeyedService<DbDataSource>(opt.ConnectionName);

        return ActivatorUtilities.CreateInstance<ProjectionStore>(services, dbSource, Options.Create(opt), name);
    }
}
