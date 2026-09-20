using System.Data;
using System.Data.Common;
using System.Diagnostics;

using Polly;

namespace Filin.EventStore.Postgres.Data;

sealed class ResilientDbConnection : DbConnection
{
    bool _disposed;
    long _subscriberCount;
    StateChangeEventHandler? _stateChangeEvent;
    readonly DbConnection _connection;
    readonly ResiliencePipeline _connectPipeline;

    public ResilientDbConnection(DbConnection connection, ResiliencePipeline connectPipeline)
    {
        _connection = connection;
        _connectPipeline = connectPipeline;

        connection.Disposed += Connection_Disposed;
    }

    void Connection_Disposed(object? sender, EventArgs e) => Debug.Assert(_disposed, "Inner connection must not be dispose before this");

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override string Database => _connection.Database;
    public override string DataSource => _connection.DataSource;
    public override string ServerVersion => _connection.ServerVersion;
    public override ConnectionState State => _connection.State;
    public override event StateChangeEventHandler? StateChange
    {
        add
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (Interlocked.Increment(ref _subscriberCount) == 1)
            {
                _connection.StateChange += OnInnerStateChanged;
            }

            StateChangeEventHandler? originalValue;
            do
            {
                originalValue = _stateChangeEvent;
            }
            while (Interlocked.CompareExchange(ref _stateChangeEvent, (StateChangeEventHandler?)Delegate.Combine(originalValue, value), originalValue) != originalValue);
        }
        remove
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            StateChangeEventHandler? originalValue;
            StateChangeEventHandler? newValue;
            do
            {
                originalValue = _stateChangeEvent;
                newValue = (StateChangeEventHandler?)Delegate.Remove(originalValue, value);
            } while (Interlocked.CompareExchange(ref _stateChangeEvent, newValue, originalValue) != originalValue);

            if (originalValue != newValue)
            {
                if (Interlocked.Decrement(ref _subscriberCount) == 0)
                {
                    _connection.StateChange -= OnInnerStateChanged;
                }
            }
        }
    }

    public override void Open() => OpenAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var context = ResilienceContextPool.Shared.Get(operationKey: "OpenDbConnection", continueOnCapturedContext: false, cancellationToken);

        try
        {
            var outcome = await _connectPipeline.ExecuteOutcomeAsync(OpenConnectionAsync, context, _connection).ConfigureAwait(false);

            outcome.ThrowIfException();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    public override void Close()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _connection.Close();
    }

    public override void ChangeDatabase(string databaseName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _connection.ChangeDatabase(databaseName);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _connection.BeginTransaction(isolationLevel);
    }

    protected override DbCommand CreateDbCommand()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ResilientDbCommand(this, _connection.CreateCommand());
    }

    protected override void Dispose(bool disposing)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _disposed = true;

        if (disposing)
        {
            var handlers = Interlocked.Exchange(ref _stateChangeEvent, null);

            if (handlers != null)
            {
                _connection.StateChange -= OnInnerStateChanged;
            }

            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    static async ValueTask<Outcome<object>> OpenConnectionAsync(ResilienceContext context, DbConnection connection)
    {
        try
        {
            await connection.OpenAsync(context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);

            return Outcome.FromResult<object>(null);
        }
        catch (Exception e)
        {
            return Outcome.FromException<object>(e);
        }
    }

    void OnInnerStateChanged(object _, StateChangeEventArgs args) => _stateChangeEvent?.Invoke(this, args);
}
