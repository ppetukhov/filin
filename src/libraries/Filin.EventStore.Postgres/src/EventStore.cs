using System.Threading.Channels;

using Filin.Configuration;
using Filin.EventStore.Postgres.DataFlow;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres;

/// <summary>
/// Реализация хранилища <see cref="IEventStore"/> в PostgreSQL.
/// </summary>
/// <param name="commandFactory"><inheritdoc cref="DbCommandFactory" path="/summary"/></param>
/// <param name="appendEventsBlock"><inheritdoc cref="AppendEventsBlock" path="/summary"/></param>
/// <param name="messageWriter"><inheritdoc cref="ChannelWriter{T}" path="/summary"/></param>
/// <param name="loggerFactory"><inheritdoc cref="ILoggerFactory" path="/summary"/></param>
/// <param name="options"><inheritdoc cref="IOptions{TOptions}" path="/summary"/></param>
sealed partial class EventStore(
    DbCommandFactory commandFactory,
    AppendEventsBlock appendEventsBlock,
    ChannelWriter<Message> messageWriter,
    ILoggerFactory loggerFactory,
    IOptions<EventStorePostgresOptions> options) : IEventStore
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres");

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return appendEventsBlock.AppendAventAsync(@event, appendCondition, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(IEnumerable<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events is not ICollection<DcbEvent> collection)
        {
            collection = [.. events];
        }

        return collection.Count switch
        {
            0 => Task.FromResult(AppendResult.Empty),
            1 => AppendAsync(collection.First(), appendCondition, cancellationToken),
            _ => AppendAsync(collection, appendCondition, cancellationToken),
        };
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> ReadAsync(DcbQuery query, EventPosition afterPosition) => new AsyncEnumerable(commandFactory, query, afterPosition, options, _logger);

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query) =>
        new StreamAsyncEnumerable(
            messageWriter: messageWriter,
            query: query,
            afterPosition: default,
            options: options,
            logger: _logger,
            storageEnumerable: null);

    /// <inheritdoc/>
    public IAsyncEnumerable<DcbEventSequenced> Subscribe(DcbQuery query, EventPosition afterPosition) =>
        new StreamAsyncEnumerable(
            messageWriter: messageWriter,
            query: query,
            afterPosition: afterPosition,
            options: options,
            logger: _logger,
            storageEnumerable: ReadAsync(query, afterPosition));

    static DcbEventSequenced InitialEnumeratorEvent(EventPosition afterPosition) => new(afterPosition, default, new DcbEvent
    {
        EventType = default,
        Payload = new object()
    });

    static DcbEventStoreException EventStoreException(Exception ex) => new(message: ex.Message, innerException: ex);

    Task<AppendResult> AppendAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken) => appendEventsBlock.AppendEventsAsync(events, appendCondition, cancellationToken);
}
