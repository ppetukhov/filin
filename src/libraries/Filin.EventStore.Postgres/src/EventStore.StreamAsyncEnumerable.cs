using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Filin.Configuration;

namespace Filin.EventStore.Postgres;

sealed partial class EventStore
{
    sealed class StreamAsyncEnumerable(
        ChannelWriter<Message> messageWriter,
        DcbQuery query,
        EventPosition afterPosition,
        IOptions<EventStorePostgresOptions> options,
        ILogger logger,
        IAsyncEnumerable<DcbEventSequenced>? storageEnumerable) : IAsyncEnumerable<DcbEventSequenced>
    {
        /// <inheritdoc/>
        public IAsyncEnumerator<DcbEventSequenced> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var enumerator = new StreamAsyncEnumerator(
                channel: CreateChannel(),
                query: query,
                initialEvent: InitialEnumeratorEvent(afterPosition),
                logger: logger,
                storeEnumerator: storageEnumerable?.GetAsyncEnumerator(cancellationToken),
                onDispose: OnDispose,
                cancellationToken: cancellationToken);

            OnSubscribe(messageWriter, enumerator, cancellationToken);

            return enumerator;
        }

        static void OnSubscribe(ChannelWriter<Message> messageWriter, StreamAsyncEnumerator subscriber, CancellationToken cancellationToken)
        {
            var streamData = Message.Subscribe(subscriber);

            while (!messageWriter.TryWrite(streamData))
            {
                Thread.SpinWait(10);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        async ValueTask OnDispose(StreamAsyncEnumerator subscriber)
        {
            var streamData = Message.Unsubscribe(subscriber);

            await messageWriter.WriteAsync(streamData).ConfigureAwait(false);
        }

        Channel<DcbEventSequenced> CreateChannel() => Channel.CreateBounded<DcbEventSequenced>(new BoundedChannelOptions(options.Value.SubscriptionEventBuffer)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }
}
