using System.Data.Common;

using Filin.AdoNet;
using Filin.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Filin.ProjectionStore.AdoNet;

static class ProjectionStoreFactory
{
    internal static ProjectionStore Create(IServiceProvider services)
    {
        var opt = services.GetRequiredService<IOptions<ProjectionStoreAdoNetOptions>>();

        var dbSource = string.IsNullOrEmpty(opt.Value.ConnectionName)
            ? DbConnectionFactory.CreateDataSource(opt.Value.Invariant, opt.Value.ConnectionString)
            : services.GetRequiredKeyedService<DbDataSource>(opt.Value.ConnectionName);

        return ActivatorUtilities.CreateInstance<ProjectionStore>(services, dbSource, opt);
    }
}
