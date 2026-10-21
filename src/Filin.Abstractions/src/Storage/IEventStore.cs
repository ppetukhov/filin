namespace Filin.Storage;

/// <summary>
/// Defines the core contract for an event store, providing capabilities to append, read, and subscribe to domain dcbEvents.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a single domain event to the store under specific execution conditions.
    /// </summary>
    /// <param name="dcbEvent">The domain event payload to persist.</param>
    /// <param name="appendCondition">The optimistic concurrency or stream conditions required for the operation to succeed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the <see cref="AppendResult"/> of the operation.</returns>
    /// <exception cref="EventStoreException">Thrown when the append operation fails due to concurrency conflicts or storage errors.</exception>
    Task<AppendResult> AppendAsync(DcbEvent dcbEvent, AppendCondition appendCondition, CancellationToken cancellationToken);

    /// <summary>
    /// Appends a batch of domain dcbEvents to the store atomically.
    /// </summary>
    /// <param name="dcbEvents">The collection of domain event payloads to persist as a single atomic transaction.</param>
    /// <param name="appendCondition">The optimistic concurrency or stream conditions required for the operation to succeed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the <see cref="AppendResult"/> of the operation.</returns>
    /// <exception cref="EventStoreException">Thrown when the batch append operation fails due to concurrency conflicts or storage errors.</exception>
    Task<AppendResult> AppendAsync(IEnumerable<DcbEvent> dcbEvents, AppendCondition appendCondition, CancellationToken cancellationToken);

    /// <summary>
    /// Reads historical sequenced dcbEvents from the store based on the specified query criteria and starting position.
    /// </summary>
    /// <param name="query">The query filters and criteria used to select relevant dcbEvents.</param>
    /// <param name="after">The exclusive stream or global position from which to start reading dcbEvents.</param>
    /// <returns>An asynchronous enumerable sequence of matching <see cref="DcbEventSequenced"/> objects.</returns>
    /// <exception cref="EventStoreException">Thrown when an error occurs while querying or streaming dcbEvents from the store.</exception>
    IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition after);

    /// <summary>
    /// Subscribes to a real-time stream of new dcbEvents that match the specified query criteria, starting after a given position.
    /// </summary>
    /// <param name="query">The query filters and criteria used to filter incoming dcbEvents.</param>
    /// <param name="after">The exclusive stream or global position from which to catch up before listening to real-time dcbEvents.</param>
    /// <returns>An asynchronous enumerable sequence that yields new <see cref="DcbEventSequenced"/> objects as they are published.</returns>
    IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition after);
}
