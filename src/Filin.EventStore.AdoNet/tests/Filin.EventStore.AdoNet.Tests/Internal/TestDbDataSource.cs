using System.Data.Common;

namespace Filin.EventStore.AdoNet.Tests.Internal;

public class TestDbDataSource : DbDataSource
{
    public override string ConnectionString => "Host=localhost;";

    protected override DbConnection CreateDbConnection() => throw new NotImplementedException();

    protected override DbCommand CreateDbCommand(string? commandText)
    {
        return new TestDbCommand { CommandText = commandText ?? string.Empty };
    }
}
