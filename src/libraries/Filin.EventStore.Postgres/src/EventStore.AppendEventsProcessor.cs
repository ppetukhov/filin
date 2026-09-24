using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

using Filin.Storage;

using Microsoft.Extensions.Logging;

namespace Filin.EventStore.Postgres;

sealed partial class EventStore
{
    readonly record struct AppendRequest(Func<DbCommand, object, CancellationToken, Task<AppendResult>> handler, DbCommand command, object state, CancellationToken cancellationToken, TaskCompletionSource<AppendResult> cts);

    sealed class AppendEventsProcessor(int boundedCapacity, ILogger logger) : IAsyncDisposable
    {
        static readonly TimeSpan[] _retryDelays =
            [
                TimeSpan.FromMilliseconds(3),
                TimeSpan.FromMilliseconds(6),
                TimeSpan.FromMilliseconds(12)
            ];

        readonly ActionBlock<AppendRequest> _actionBlock = new(async request =>
        {
            var (handler, command, state, cancellationToken, cts) = request;

            var retry = 0;

            TimeSpan delay = default;

            using (command)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.AppendingEvents();
                }

                while (true)
                {
                    try
                    {
                        if (delay != default)
                        {
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(true);
                        }

                        var result = await handler(command, state, cancellationToken).ConfigureAwait(true);

                        cts.TrySetResult(result);
                        return;
                    }
                    catch (DbException e) when (e.IsTransient && retry < _retryDelays.Length)
                    {
                        delay = _retryDelays[retry++];

                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.EventStoreConnectingDelay(delay);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            logger.AppendingEventsCancelled();
                        }

                        cts.TrySetCanceled();

                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorAppendEvents(ex);

                        cts.TrySetException(ex);

                        return;
                    }
                }
            }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = boundedCapacity
        });

        public ValueTask DisposeAsync()
        {
            _actionBlock.Complete();

            return new ValueTask(_actionBlock.Completion);
        }

        internal Task<AppendResult> AppendEvent(DbCommand command, DcbEvent @event, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<AppendResult>();

            var req = new AppendRequest(AppendEventInner, command, @event, cancellationToken, tcs);

            var posted = _actionBlock.Post(req);

            if (!posted)
            {
                ThrowSystemIsToBusy();
            }

            return tcs.Task;
        }

        internal Task<AppendResult> AppendEvents(DbCommand command, ICollection<DcbEvent> events, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<AppendResult>();

            var req = new AppendRequest(AppendEventsInner, command, events, cancellationToken, tcs);

            var posted = _actionBlock.Post(req);

            if (!posted)
            {
                ThrowSystemIsToBusy();
            }

            return tcs.Task;
        }

        async Task<AppendResult> AppendEventInner(DbCommand command, object state, CancellationToken cancellationToken)
        {
            DcbEvent @event = (DcbEvent)state;

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.ConflictAppendingEvents();

                return AppendResult.Conflict;
            }

            var position = new EventPosition((ulong)reader.GetInt64(reader.GetOrdinal(ColumnNames.Position)));
            var timestamp = new DateTime(reader.GetDateTime(reader.GetOrdinal(ColumnNames.CreatedAt)).Ticks, DateTimeKind.Utc);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.EventsAppended();
            }

            return new(new DcbEventSequenced(position, timestamp, @event));
        }

        async Task<AppendResult> AppendEventsInner(DbCommand command, object state, CancellationToken cancellationToken)
        {
            var events = (ICollection<DcbEvent>)state;

            var count = events.Count;

            var positions = new List<(EventPosition, DateTimeOffset)>(capacity: count);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var positionOrdinal = reader.GetOrdinal(ColumnNames.Position);
            var createAtOrdinal = reader.GetOrdinal(ColumnNames.CreatedAt);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var position = new EventPosition((ulong)reader.GetInt64(positionOrdinal));
                var timestamp = new DateTime(reader.GetDateTime(createAtOrdinal).Ticks, DateTimeKind.Utc);

                positions.Add((position, timestamp));
            }

            if (positions.Count == 0)
            {
                logger.ConflictAppendingEvents();

                return AppendResult.Conflict;
            }

            if (positions.Count != count)
            {
                ThrowUnexpectedRowsCount(count, positions.Count);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.EventsAppended();
            }

            var items = new DcbEventSequenced[count];

            var i = 0;

            foreach (var evt in events)
            {
                var (position, timestamp) = positions[i];

                items[i] = new(position, timestamp, evt);
                i++;
            }

            return new(items);
        }

        [DoesNotReturn]
        static void ThrowUnexpectedRowsCount(int expectedCount, int count)
        {
            var ex = new DcbEventStoreException("Unexpected rows count");

            ex.Data["Count"] = count;
            ex.Data["ExpectedCount"] = expectedCount;

            throw ex;
        }

        [DoesNotReturn]
        static void ThrowSystemIsToBusy() => throw new DcbEventStoreException("System is too busy.");
    }
}
