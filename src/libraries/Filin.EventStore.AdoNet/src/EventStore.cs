using System.Threading.Channels;

using Filin.Configuration;
using Filin.EventStore.AdoNet.DataFlow;
using Filin.EventStore.AdoNet.Internal;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.AdoNet;

/// <summary>
/// A AdoNet-based implementation of the <see cref="IEventStore"/> interface.
/// </summary>
/// <param name="commandFactory">The factory used to create and configure database commands.</param>
/// <param name="appendEventsBlock"><inheritdoc cref="AppendEventsBlock" path="/summary"/></param>
/// <param name="messageWriter">The channel writer used to dispatch outbound messages or dcbEvents asynchronously.</param>
/// <param name="loggerFactory">The factory used to create loggers for tracing store operations.</param>
/// <param name="options">The configuration options specific to the event store implementation.</param>
sealed partial class EventStore(
    DbCommandFactory commandFactory,
    IAppendEventsBlock appendEventsBlock,
    ChannelWriter<EventStreamMessage> messageWriter,
    ILoggerFactory loggerFactory,
    IOptions<EventStoreAdoNetOptions> options) : IEventStore, IDisposable
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.EventStore.AdoNet");
    readonly AppendEventsBlockWrapper _appendEventsBlockWrapped = new(appendEventsBlock);

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(DcbEvent dcbEvent, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dcbEvent);

        return _appendEventsBlockWrapped.AppendAventAsync(dcbEvent, appendCondition, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AppendResult> AppendAsync(IEnumerable<DcbEvent> dcbEvents, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dcbEvents);

        if (dcbEvents is not ICollection<DcbEvent> collection)
        {
            collection = [.. dcbEvents];
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

    /// <inheritdoc/>
    public void Dispose() => _appendEventsBlockWrapped.Dispose();

    static DcbEventSequenced InitialEnumeratorEvent(EventPosition afterPosition) => new(afterPosition, default, new DcbEvent
    {
        EventType = default,
        Payload = new object()
    });

    static EventStoreException EventStoreException(Exception ex) => new(message: ex.Message, innerException: ex);

    Task<AppendResult> AppendAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken) => _appendEventsBlockWrapped.AppendEventsAsync(events, appendCondition, cancellationToken);
}
