#nullable disable
using System.Data;
using System.Data.Common;

namespace Filin.EventStore.AdoNet.Tests.Internal;

sealed class TestDbCommand : DbCommand
{
    readonly TestDbParameterCollection _parameters = [];

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction DbTransaction { get; set; }

    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new TestDbParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
}

#nullable restore
