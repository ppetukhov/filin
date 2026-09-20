using System.Data.Common;

using Filin.AdoNet;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Filin.Hosting;

static class DbDataSourseServiceCollectionExtensions
{
    internal static IServiceCollection TryAddDbDataSource(this IServiceCollection services, string invariant, string connectionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(invariant);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        services.TryAddKeyedSingleton<DbDataSource>(connectionName, (sp, key) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString((string)key!) ?? throw new FilinConfigurationException($"ConnectionString '{key}' not found");

            return DbConnectionFactory.CreateDataSource(invariant, connectionString);
        });

        return services;
    }
}
