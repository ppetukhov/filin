using System.Data.Common;

namespace Filin.EventStore.AdoNet.Tests.Internal;

sealed class TestDbDataSource : DbDataSource
{
    public override string ConnectionString => "Host=localhost;";

    protected override DbConnection CreateDbConnection() => throw new NotImplementedException();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
    protected override DbCommand CreateDbCommand(string? commandText) => new TestDbCommand { CommandText = commandText ?? string.Empty };
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
}
