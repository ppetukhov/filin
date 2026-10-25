#nullable disable
using Filin.Storage;
using Filin.Storage.DataFlow;

namespace Filin.EventStore.AdoNet.Tests.Internal;

public class AppendBlockTestContext : IContext
{
    public TaskCompletionSource<AppendResult> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    public Exception Exception { get; private set; }
    public bool IsCanceled { get; private set; }

    public bool TrySetCanceled(CancellationToken cancellationToken)
    {
        IsCanceled = true;
        return Tcs.TrySetCanceled(cancellationToken);
    }

    public bool TrySetException(Exception exception)
    {
        Exception = exception;
        return Tcs.TrySetException(exception);
    }
}

#nullable restore
