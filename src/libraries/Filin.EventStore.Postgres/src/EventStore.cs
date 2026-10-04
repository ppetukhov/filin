using System.Threading.Channels;

using Filin.Configuration;
using Filin.EventStore.Postgres.DataFlow;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres;

/// <summary>
/// A PostgreSQL-based implementation of the <see cref="IEventStore"/> interface.
/// </summary>
/// <param name="commandFactory">The factory used to create and configure database commands for PostgreSQL.</param>
/// <param name="appendEventsBlock"><inheritdoc cref="AppendEventsBlock" path="/summary"/></param>
/// <param name="messageWriter">The channel writer used to dispatch outbound messages or events asynchronously.</param>
/// <param name="loggerFactory">The factory used to create loggers for tracing store operations.</param>
/// <param name="options">The configuration options specific to the PostgreSQL event store implementation.</param>
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
