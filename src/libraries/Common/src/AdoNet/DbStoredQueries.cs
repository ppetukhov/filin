using System.Data;
using System.Globalization;
using System.Reflection;

namespace Filin.AdoNet;

/// <summary>
/// This class implements the expected contract between Orleans and the underlying relational storage.
/// It makes sure all the stored queries are present and 
/// </summary>
sealed class DbStoredQueries
{
#pragma warning disable IDE0040 // Remove accessibility modifiers
#pragma warning disable IDE0052 // Remove unread private members
    private readonly Dictionary<string, string> _queries;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore IDE0040 // Remove accessibility modifiers

    internal DbStoredQueries(Dictionary<string, string> queries)
    {
        var fields = typeof(DbStoredQueries).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(p => p.Name);
        var missingQueryKeys = fields.Except(queries.Keys).ToArray();
        if (missingQueryKeys.Length > 0)
        {
            throw new ArgumentException(
                $"Not all required queries found. Missing are: {string.Join(",", missingQueryKeys)}");
        }
        _queries = queries;
    }

    /// <summary>
    /// The query that's used to get all the stored queries.
    /// this will probably be the same for all relational dbs.
    /// </summary>
    internal const string GetQueriesKey = "SELECT QueryKey, QueryText FROM FilinQuery";

    internal static class Converters
    {
        internal static KeyValuePair<string, string> GetQueryKeyAndValue(IDataRecord record) =>
            new(record.GetValue<string>("QueryKey"),
                record.GetValue<string>("QueryText"));

        internal static int GetVersion(IDataRecord record) =>
            Convert.ToInt32(record.GetValue<object>(nameof(Version)), CultureInfo.InvariantCulture);

        internal static bool GetSingleBooleanValue(IDataRecord record) =>
            record.FieldCount != 1
                ? throw new InvalidOperationException("Expected a single column")
                : Convert.ToBoolean(record.GetValue(0), CultureInfo.InvariantCulture);
    }

    internal sealed class Columns(IDbCommand command)
    {
        void Add<T>(string paramName, T paramValue, DbType? dbType = null) => command.AddParameter(paramName, paramValue, dbType: dbType);

        internal long MessageId
        {
            set => Add(nameof(MessageId), value);
        }

        internal byte[] Payload
        {
            set => Add(nameof(Payload), value);
        }

        internal string Items
        {
            set => Add(nameof(Items), value);
        }
    }
}
