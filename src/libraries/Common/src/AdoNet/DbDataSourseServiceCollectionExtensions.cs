using System.Data.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Filin.AdoNet;

static class DbDataSourseServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection TryAddDbDataSource(string invariant, string connectionName)
        {
            ArgumentException.ThrowIfNullOrEmpty(invariant);
            ArgumentException.ThrowIfNullOrEmpty(connectionName);

            services.TryAddKeyedSingleton<DbDataSource>(connectionName, (sp, key) =>
            {
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString((string)key!) ?? throw new ConfigurationException($"ConnectionString '{key}' not found");

                return DbConnectionFactory.CreateDataSource(invariant, connectionString);
            });

            return services;
        }
    }
}
