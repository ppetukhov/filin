using System.Data;

namespace Orca.AdoNet;

/// <summary>
/// A class for all relational storages that support all systems stores : membership, reminders and statistics
/// </summary>
sealed class RelationalOrleansQueries
{
    /// <summary>
    /// the underlying storage
    /// </summary>
    readonly IRelationalStorage _storage;

    /// <summary>
    /// the orleans functional queries
    /// </summary>
    readonly DbStoredQueries _dbStoredQueries;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="storage">the underlying relational storage</param>
    /// <param name="dbStoredQueries">Orleans functional queries</param>
    RelationalOrleansQueries(IRelationalStorage storage, DbStoredQueries dbStoredQueries)
    {
        _storage = storage;
        _dbStoredQueries = dbStoredQueries;
    }

    /// <summary>
    /// Creates an instance of a database of type <see cref="RelationalOrleansQueries"/> and Initializes Orleans queries from the database.
    /// Orleans uses only these queries and the variables therein, nothing more.
    /// </summary>
    /// <param name="invariantName">The invariant name of the connector for this database.</param>
    /// <param name="connectionString">The connection string this database should use for database operations.</param>
    internal static async Task<RelationalOrleansQueries> CreateInstance(string invariantName, string connectionString)
    {
        var storage = RelationalStorage.CreateInstance(invariantName, connectionString);

        var queries = await storage.ReadAsync(DbStoredQueries.GetQueriesKey, DbStoredQueries.Converters.GetQueryKeyAndValue, null).ConfigureAwait(false);

        return new RelationalOrleansQueries(storage, new DbStoredQueries(queries.ToDictionary(q => q.Key, q => q.Value)));
    }

    Task<int> ExecuteAsync(string query, Func<IDbCommand, DbStoredQueries.Columns> parameterProvider) => _storage.ExecuteAsync(query, command => parameterProvider(command));

    async Task<TAggregate> ReadAsync<TResult, TAggregate>(string query,
        Func<IDataRecord, TResult> selector,
        Func<IDbCommand, DbStoredQueries.Columns> parameterProvider,
        Func<IEnumerable<TResult>, TAggregate> aggregator)
    {
        var ret = await _storage.ReadAsync(query, selector, command => parameterProvider(command)).ConfigureAwait(false);
        return aggregator(ret);
    }
}
