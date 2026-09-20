using System.Data.Common;

using Polly;

namespace Filin.EventStore.Postgres.Data;

sealed class ResilientDbDataSource(DbDataSource innerDataSource, ResiliencePipeline pipeline) : DbDataSource
{
    public override string ConnectionString => innerDataSource.ConnectionString;

    protected override DbConnection CreateDbConnection() => new ResilientDbConnection(innerDataSource.CreateConnection(), pipeline);

    protected override DbCommand CreateDbCommand(string? commandText = null) => new DataSourceResilientDbCommand(CreateDbConnection());

    protected override void Dispose(bool disposing)
    {
        if (disposing) innerDataSource.Dispose();
        base.Dispose(disposing);
    }
}
