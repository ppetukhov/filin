#nullable disable
using System.Data;
using System.Data.Common;

namespace Filin.EventStore.AdoNet.Tests.Internal;

public class AppendBlockTestDbCommand : DbCommand
{
    public DbDataReader ReaderToReturn { get; set; }
    public Exception ExceptionToThrow { get; set; }

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult<DbDataReader>(ReaderToReturn ?? new AppendBlockTestDbDataReader());
    }

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
    protected override DbTransaction DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => throw new NotImplementedException();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
}

#nullable restore
