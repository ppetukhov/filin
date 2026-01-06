using System.Data;
using System.Data.Common;

using Microsoft.Extensions.Logging;

using Filin.Configuration;

namespace Filin.EventStore.Postgres;

static class DbConnectionExtensions
{
    internal static async Task ConnectToEventStoreAsync(
        this DbConnection connection,
        EventStorePostgresBackoff backoff,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var reconnectCount = 0;

        while (connection.State != ConnectionState.Open)
        {
            if (reconnectCount > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var delay = TimeSpan.FromSeconds(Math.Pow(Math.Abs(backoff.Exponent), reconnectCount));

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
