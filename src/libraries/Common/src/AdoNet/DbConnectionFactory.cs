using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Filin.AdoNet;

/// This class caches the references to all loaded factories
static class DbConnectionFactory
{
    static readonly ConcurrentDictionary<string, CachedFactory> FactoryCache =
        new();

    static readonly Dictionary<string, List<Tuple<string, string>>> ProviderFactoryTypeMap =
        new()
        {
            { AdoNetInvariants.InvariantNameSqlServer, new List<Tuple<string, string>>{ new("System.Data.SqlClient", "System.Data.SqlClient.SqlClientFactory") } },
            { AdoNetInvariants.InvariantNameMySql, new List<Tuple<string, string>>{ new("MySql.Data", "MySql.Data.MySqlClient.MySqlClientFactory") } },
            { AdoNetInvariants.InvariantNamePostgreSql, new List<Tuple<string, string>>{ new("Npgsql", "Npgsql.NpgsqlFactory") } },
            { AdoNetInvariants.InvariantNameSqlLite, new List<Tuple<string, string>>{ new("Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite.SqliteFactory") } },
            { AdoNetInvariants.InvariantNameSqlServerDotnetCore,new List<Tuple<string, string>>{ new("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient.SqlClientFactory") } },
            { AdoNetInvariants.InvariantNameMySqlConnector, new List<Tuple<string, string>>{ new("MySqlConnector", "MySqlConnector.MySqlConnectorFactory") , new("MySqlConnector", "MySql.Data.MySqlClient.MySqlClientFactory") } },
        };

    [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetType(String)")]
    static CachedFactory GetFactory(string invariantName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invariantName);

        if (!ProviderFactoryTypeMap.TryGetValue(invariantName, out var providerFactoryDefinitions) || providerFactoryDefinitions.Count == 0)
            throw new InvalidOperationException($"Database provider factory with '{invariantName}' invariant name not supported.");

        List<Exception>? exceptions = null;

        foreach (var providerFactoryDefinition in providerFactoryDefinitions)
        {
            Assembly? asm = null;
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                var asmName = new AssemblyName(providerFactoryDefinition.Item1);
                asm = Assembly.Load(asmName);
            }
            catch (Exception ex)
            {
                AddException(new InvalidOperationException($"Unable to find and/or load a candidate assembly '{providerFactoryDefinition.Item1}' for '{invariantName}' invariant name.", ex));
                continue;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            if (asm is null)
            {
                AddException(new InvalidOperationException($"Can't find database provider factory with '{invariantName}' invariant name. Please make sure that your ADO.Net provider package library is deployed with your application."));
                continue;
            }

            var providerFactoryType = asm.GetType(providerFactoryDefinition.Item2);
            if (providerFactoryType is null)
            {
                AddException(new InvalidOperationException($"Unable to load type '{providerFactoryDefinition.Item2}' for '{invariantName}' invariant name."));
                continue;
            }

            var prop = providerFactoryType.GetFields().SingleOrDefault(p => string.Equals(p.Name, "Instance", StringComparison.OrdinalIgnoreCase) && p.IsStatic);
            if (prop is null)
            {
                AddException(new InvalidOperationException($"Invalid provider type '{providerFactoryDefinition.Item2}' for '{invariantName}' invariant name."));
                continue;
            }

            var factory = (DbProviderFactory)prop.GetValue(null)!;
            return new CachedFactory(factory, providerFactoryType.Name, "", providerFactoryType.AssemblyQualifiedName!);
        }

        throw new AggregateException(exceptions!);

        void AddException(Exception ex)
        {
            exceptions ??= new List<Exception>();
            exceptions.Add(ex);
        }
    }

    public static DbConnection CreateConnection(string invariantName, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invariantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var factory = FactoryCache.GetOrAdd(invariantName, GetFactory).Factory;
        var connection = factory.CreateConnection() ?? throw new InvalidOperationException($"Database provider factory: '{invariantName}' did not return a connection object.");
        connection.ConnectionString = connectionString;
        return connection;
    }

    sealed class CachedFactory(DbProviderFactory factory, string factoryName, string factoryDescription, string factoryAssemblyQualifiedNameKey)
    {
        /// <summary>
        /// The factory to provide vendor specific functionality.
        /// </summary>
        /// <remarks>For more about <see href="http://florianreischl.blogspot.fi/2011/08/adonet-connection-pooling-internals-and.html">ConnectionPool</see>
        /// and issues with using this factory. Take these notes into account when considering robustness of Orleans!</remarks>
        public readonly DbProviderFactory Factory = factory;

        /// <summary>
        /// The name of the loaded factory, set by a database connector vendor.
        /// </summary>
        public readonly string FactoryName = factoryName;

        /// <summary>
        /// The description of the loaded factory, set by a database connector vendor.
        /// </summary>
        public readonly string FactoryDescription = factoryDescription;

        /// <summary>
        /// The description of the loaded factory, set by a database connector vendor.
        /// </summary>
        public readonly string FactoryAssemblyQualifiedNameKey = factoryAssemblyQualifiedNameKey;
    }
}
