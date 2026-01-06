using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Filin.EventStore.Postgres;

sealed partial class EventStore
{
    internal sealed class StreamAsyncEnumerator(
        Channel<DcbEventSequenced> channel,
        DcbQuery query,
        DcbEventSequenced initialEvent,
        ILogger logger,
        IAsyncEnumerator<DcbEventSequenced>? storeEnumerator,
        Func<StreamAsyncEnumerator, ValueTask>? onDispose,
        CancellationToken cancellationToken) : IAsyncEnumerator<DcbEventSequenced>
    {
        bool _disposed;
        long _readCount;

        internal DcbQuery Query => query;

        internal bool IsActive => !_disposed && !cancellationToken.IsCancellationRequested;

        /// <inheritdoc/>
        public DcbEventSequenced Current { get; private set; } = initialEvent;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.SuppressFinalize(this);

            if (Interlocked.Exchange(ref storeEnumerator, null) is IAsyncEnumerator<DcbEventSequenced> disp)
            {
                try
                {
                    await disp.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                }
            }

            if (Interlocked.Exchange(ref onDispose, null) is Func<StreamAsyncEnumerator, ValueTask> onDisp)
            {
                try
                {
                    await onDisp(this).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (storeEnumerator is IAsyncEnumerator<DcbEventSequenced> enumerator)
                {
                    var success = await enumerator.MoveNextAsync().ConfigureAwait(false);

                    if (success)
                    {
                        _ = Interlocked.Increment(ref _readCount);

                        Current = enumerator.Current;
                        return true;
                    }
                    else if (Interlocked.Exchange(ref storeEnumerator, null) is IAsyncEnumerator<DcbEventSequenced> disp)
                    {
                        await disp.DisposeAsync().ConfigureAwait(false);
                    }
                }

                DcbEventSequenced evt;

                do
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.ReadingEventsCancelled(_readCount);
                        }

                        return false;
                    }

                    evt = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                while (evt.Position <= Current.Position);

                _ = Interlocked.Increment(ref _readCount);

                Current = evt;
            }
            catch (OperationCanceledException)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.ReadingEventsCancelled(_readCount);
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.ErrorReadingEvents(ex);

                throw EventStoreException(ex);
            }

            return true;
        }

        internal bool CanAdd(DcbEventSequenced eventSequenced) => IsActive && eventSequenced.Position > Current.Position && query.MatchesEvent(eventSequenced.Event);

        internal ValueTask UnsafeAddEvent(DcbEventSequenced eventSequenced) =>
            IsActive && eventSequenced.Position > Current.Position
            ? channel.Writer.WriteAsync(eventSequenced, cancellationToken)
            : ValueTask.CompletedTask;
    }
}
