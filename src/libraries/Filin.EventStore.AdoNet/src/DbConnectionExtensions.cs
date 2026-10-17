using System.Data;
using System.Data.Common;

using Filin.Configuration;

using Microsoft.Extensions.Logging;

namespace Filin.EventStore.AdoNet;

static class DbConnectionExtensions
{
    extension(DbConnection connection)
    {
        internal async Task ConnectToEventStoreAsync(
            EventStoreAdoNetBackoff backoff,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var reconnectCount = 0;

            while (connection.State != ConnectionState.Open)
            {
                if (reconnectCount > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var delay = backoff.InitalDelay * Math.Pow(backoff.Exponent, reconnectCount);

                    if (backoff.UseJitter)
                    {
#pragma warning disable CA5394 // Do not use insecure randomness
                        delay += TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * 10);
#pragma warning restore CA5394 // Do not use insecure randomness
                    }

                    if (backoff.MaxDelay < delay)
                    {
                        delay = backoff.MaxDelay;
                    }

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.EventStoreConnectingDelay(delay);
                    }

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                if (reconnectCount != 0)
                {
                    logger.EventStoreConnectingRetry(reconnectCount);
                }
                else if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.EventStoreConnecting();
                }

                try
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    if (reconnectCount != 0)
                    {
                        logger.EventStoreReconnected();
                    }
                    else if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.EventStoreConnected();
                    }

                    reconnectCount = 0;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.EventStoreConnectError(e);

                    reconnectCount += 1;

                    continue;
                }
            }
        }
    }
}
