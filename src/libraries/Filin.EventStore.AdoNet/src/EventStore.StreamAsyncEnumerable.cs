using System.Threading.Channels;

using Filin.Configuration;
using Filin.EventStore.AdoNet.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.AdoNet;

sealed partial class EventStore
{
    sealed class StreamAsyncEnumerable(
        ChannelWriter<EventStreamMessage> messageWriter,
        DcbQuery query,
        EventPosition afterPosition,
        IOptions<EventStoreAdoNetOptions> options,
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

        static void OnSubscribe(ChannelWriter<EventStreamMessage> messageWriter, StreamAsyncEnumerator subscriber, CancellationToken cancellationToken)
        {
            var streamData = EventStreamMessage.Subscribe(subscriber);

            while (!messageWriter.TryWrite(streamData))
            {
                Thread.SpinWait(10);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        async ValueTask OnDispose(StreamAsyncEnumerator subscriber)
        {
            var streamData = EventStreamMessage.Unsubscribe(subscriber);

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
