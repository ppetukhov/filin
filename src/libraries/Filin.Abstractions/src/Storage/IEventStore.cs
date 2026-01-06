namespace Filin.Storage;

/// <summary>
/// EventStore.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query) => ReadAsync(query, default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="afterPosition"></param>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition afterPosition);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="domainEvent"></param>
    /// <param name="failIfEventsMatch"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="DcbConflictException"></exception>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<EventPosition> AppendAsync(DcbEvent domainEvent, DcbQuery failIfEventsMatch, CancellationToken cancellationToken = default) => AppendAsync(domainEvent, failIfEventsMatch, default, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="domainEvent"></param>
    /// <param name="failIfEventsMatch"></param>
    /// <param name="after"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="DcbConflictException"></exception>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<EventPosition> AppendAsync(DcbEvent domainEvent, DcbQuery failIfEventsMatch, EventPosition after, CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="domainEvents"></param>
    /// <param name="failIfEventsMatch"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="domainEvents"/> is empty.</exception>
    /// <exception cref="DcbConflictException"></exception>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<IEnumerable<EventPosition>> AppendAsync(IEnumerable<DcbEvent> domainEvents, DcbQuery failIfEventsMatch, CancellationToken cancellationToken = default) => AppendAsync(domainEvents, failIfEventsMatch, default, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="domainEvents"></param>
    /// <param name="failIfEventsMatch"></param>
    /// <param name="after"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="domainEvents"/> is empty.</exception>
    /// <exception cref="DcbConflictException"></exception>
    /// <exception cref="DcbEventStoreException"></exception>
    /// <returns></returns>
    Task<IEnumerable<EventPosition>> AppendAsync(IEnumerable<DcbEvent> domainEvents, DcbQuery failIfEventsMatch, EventPosition after, CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="afterPosition"></param>
    /// <returns></returns>
    IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition afterPosition);
}
