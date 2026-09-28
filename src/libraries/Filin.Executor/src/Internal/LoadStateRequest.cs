using Filin.DataFlow;

namespace Filin.Internal;

abstract class LoadStateRequest<TState>(ProjectionId projectionId, DcbQuery query) : IContext
    where TState : notnull
{
    public ProjectionId ProjectionId => projectionId;

    public DcbQuery Query => query;

    public abstract CancellationToken CancellationToken { get; }

    public abstract bool TrySetCanceled(CancellationToken cancellationToken);
    public abstract bool TrySetException(Exception exception);
};
