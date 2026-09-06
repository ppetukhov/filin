namespace Filin.Storage;

/// <summary>
/// EventStore.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="appendCondition"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<AppendResult> AppendAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="events"></param>
    /// <param name="appendCondition"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<AppendResult> AppendAsync(IEnumerable<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="after"></param>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition after);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition after);
}
