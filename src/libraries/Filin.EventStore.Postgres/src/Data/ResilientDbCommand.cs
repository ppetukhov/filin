using System.Data;
using System.Data.Common;

namespace Filin.EventStore.Postgres.Data;

class ResilientDbCommand(DbConnection conn, DbCommand inner) : DbCommand
{
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
    public override CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
    protected override DbConnection? DbConnection { get { return conn; } set { conn = value!; } }
    protected override DbParameterCollection DbParameterCollection => inner.Parameters;
    protected override DbTransaction? DbTransaction { get => inner.Transaction; set => inner.Transaction = value; }
    public override void Cancel() => inner.Cancel();
    public override int ExecuteNonQuery() => inner.ExecuteNonQuery();
    public override object? ExecuteScalar() => inner.ExecuteScalar();
    public override void Prepare() => inner.Prepare();
    protected override DbParameter CreateDbParameter() => inner.CreateParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => ExecuteReaderInternal(behavior, CancellationToken.None).GetAwaiter().GetResult();
    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => ExecuteReaderInternal(behavior, cancellationToken);

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => inner.ExecuteNonQueryAsync(cancellationToken);

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => inner.ExecuteScalarAsync(cancellationToken);

    public override Task PrepareAsync(CancellationToken cancellationToken = default) => inner.PrepareAsync(cancellationToken);

    public override async ValueTask DisposeAsync()
    {
        await conn.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected virtual async Task<DbDataReader> ExecuteReaderInternal(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        try
        {
            return await inner.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (behavior.HasFlag(CommandBehavior.CloseConnection))
            {
                try
                {
                    await conn.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { inner.Dispose(); }
        base.Dispose(disposing);
    }
}
