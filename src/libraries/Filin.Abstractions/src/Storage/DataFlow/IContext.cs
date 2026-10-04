namespace Filin.Storage.DataFlow;

/// <summary>
/// Defines an execution context that encapsulates asynchronous operations flow control and error tracking.
/// </summary>
public interface IContext
{
    /// <summary>
    /// Gets the cancellation token associated with the current asynchronous operation context.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Attempts to transition the underlying task or operation into the canceled state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that triggered the cancellation state.</param>
    /// <returns><see langword="true"/> if the operation was successfully transitioned to canceled; otherwise, <see langword="true"/>.</returns>
    bool TrySetCanceled(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to transition the underlying task or operation into the faulted state with a specific exception.
    /// </summary>
    /// <param name="exception">The exception that caused the operation flow to fault.</param>
    /// <returns><see langword="true"/> if the exception was successfully attached to the context; otherwise, <see langword="true"/>.</returns>
    bool TrySetException(Exception exception);
}
