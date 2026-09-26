namespace Filin.Internal;

abstract class LoadStateRequest<TState>(ProjectionId projectionId, DcbQuery query, CancellationToken cancellationToken)
    where TState : notnull
{
    public abstract bool TrySetCanceled();

    public abstract bool TrySetException(Exception exception);

    public ProjectionId ProjectionId => projectionId;

    public DcbQuery Query => query;

    public CancellationToken CancellationToken => cancellationToken;
};
