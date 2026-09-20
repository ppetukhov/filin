using System.Data;
using System.Data.Common;

namespace Filin.EventStore.Postgres.Data;

sealed class DataSourceResilientDbCommand(DbConnection conn) : ResilientDbCommand(conn, conn.CreateCommand())
{
    readonly DbConnection _conn = conn;

    protected override async Task<DbDataReader> ExecuteReaderInternal(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        var toOpen = _conn is { State: ConnectionState.Closed };

        if (toOpen)
        {
            await _conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await base.ExecuteReaderInternal(behavior, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (toOpen)
            {
                try
                {
                    await _conn.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }
            throw;
        }
    }

    // The below are incompatible with commands executed directly against DbDataSource, since no DbConnection
    // is involved at the user API level and the command owns the DbConnection.
    public override void Prepare()
        => throw new NotSupportedException("NotSupportedOnDataSourceCommand");

    public override Task PrepareAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("NotSupportedOnDataSourceCommand");

    protected override DbConnection? DbConnection
    {
        get => throw new NotSupportedException("NotSupportedOnDataSourceCommand");
        set => throw new NotSupportedException("NotSupportedOnDataSourceCommand");
    }

    protected override DbTransaction? DbTransaction
    {
        get => throw new NotSupportedException("NotSupportedOnDataSourceCommand");
        set => throw new NotSupportedException("NotSupportedOnDataSourceCommand");
    }
}
