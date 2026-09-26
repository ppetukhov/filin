using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

using Filin.Configuration;
using Filin.DataFlow;
using Filin.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres.DataFlow;

sealed class AppendEventsBlock : IAppendEventsBlock
{
    readonly record struct ExecuteRequest(Func<DbCommand, object, CancellationToken, Task<AppendResult>> handler, DbCommand command, object state, object context, CancellationToken cancellationToken);

    static readonly TimeSpan[] _retryDelays = [.. Enumerable.Range(0, 15).Select(i => double.Pow(2.0, i) * TimeSpan.FromMilliseconds(3))];

    readonly TransformBlock<AppendEventRequest, ExecuteRequest> _appendEventBlock;
    readonly TransformBlock<AppendEventsRequest, ExecuteRequest> _appendEventsBlock;
    readonly TransformBlock<ExecuteRequest, AppendEventsResponse> _executeBlock;
    readonly ILogger _logger;

    public Task Completion => _executeBlock.Completion;

    public AppendEventsBlock(DbDataSource dataSource, IOptions<EventStorePostgresOptions> options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres");

        var execOpt = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        _appendEventBlock = new(request =>
        {
            var (@event, appendCondition, context, cancellationToken) = request;

            var command = EventStore.CreateAppendEventCommand(dataSource, @event, ref appendCondition, options.Value.StorageSerializer);

            return new ExecuteRequest { handler = AppendEventInner, command = command, state = @event, context = context, cancellationToken = cancellationToken };
        }, execOpt);

        _appendEventsBlock = new(request =>
        {
            var (events, appendCondition, context, cancellationToken) = request;

            var command = EventStore.CreateAppendEventsCommand(dataSource, events, ref appendCondition, options.Value.StorageSerializer);

            return new ExecuteRequest { handler = AppendEventsInner, command = command, state = events, context = context, cancellationToken = cancellationToken };
        }, execOpt);

        _executeBlock = new(async request =>
        {
            var (handler, command, state, context, cancellationToken) = request;

            var retry = 0;

            TimeSpan delay = default;

            using (command)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.AppendingEvents();
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

                        return new AppendEventsResponse(result, context);
                    }
                    catch (DbException e) when (e.IsTransient)
                    {
                        if (retry < _retryDelays.Length)
                        {
                            delay = _retryDelays[retry];
                        }

                        retry++;

                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.EventStoreConnectingDelay(delay);
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.AppendingEventsCancelled();
                        }

                        return new AppendEventsResponse(new AppendResult(ex), context);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorAppendEvents(ex);

                        return new AppendEventsResponse(new AppendResult(ex), context);
                    }
                }
            }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = options.Value.AppendBoundedCapacity
        });

        var linkOption = new DataflowLinkOptions { PropagateCompletion = true };

        _ = _appendEventBlock.LinkTo(_executeBlock, linkOption);
        _ = _appendEventsBlock.LinkTo(_executeBlock, linkOption);

        static bool IsLocal(AppendEventsResponse response) => response.Context is LocalContext;

        _ = _executeBlock.LinkTo(new ActionBlock<AppendEventsResponse>(_ => { }), linkOption, IsLocal); // drop
    }

    internal Task<AppendResult> AppendAventAsync(DcbEvent @event, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<AppendResult>();
        var context = new LocalContext(tcs);

        var req = new AppendEventRequest(@event, appendCondition, context, cancellationToken);

        var posted = _appendEventBlock.Post(req);

        if (!posted)
        {
            tcs.TrySetException(SystemIsToBusyException());
        }

        return tcs.Task;
    }

    internal Task<AppendResult> AppendEventsAsync(ICollection<DcbEvent> events, AppendCondition appendCondition, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<AppendResult>();
        var context = new LocalContext(tcs);

        var req = new AppendEventsRequest(events, appendCondition, context, cancellationToken);

        var posted = _appendEventsBlock.Post(req);

        if (!posted)
        {
            tcs.TrySetException(SystemIsToBusyException());
        }

        return tcs.Task;
    }

    async Task<AppendResult> AppendEventInner(DbCommand command, object state, CancellationToken cancellationToken)
    {
        DcbEvent @event = (DcbEvent)state;

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.ConflictAppendingEvents();

            return AppendResult.Conflict;
        }

        var position = new EventPosition((ulong)reader.GetInt64(reader.GetOrdinal(ColumnNames.Position)));
        var timestamp = new DateTime(reader.GetDateTime(reader.GetOrdinal(ColumnNames.CreatedAt)).Ticks, DateTimeKind.Utc);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.EventsAppended();
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
            _logger.ConflictAppendingEvents();

            return AppendResult.Conflict;
        }

        if (positions.Count != count)
        {
            ThrowUnexpectedRowsCount(count, positions.Count);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.EventsAppended();
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

    static DcbEventStoreException SystemIsToBusyException() => new DcbEventStoreException("System is too busy.");

    [DoesNotReturn]
    static void ThrowUnexpectedRowsCount(int expectedCount, int count)
    {
        var ex = new DcbEventStoreException("Unexpected rows count");

        ex.Data["Count"] = count;
        ex.Data["ExpectedCount"] = expectedCount;

        throw ex;
    }

    AppendEventsResponse ISourceBlock<AppendEventsResponse>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<AppendEventsResponse> target, out bool messageConsumed) => ((ISourceBlock<AppendEventsResponse>)_executeBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
    IDisposable ISourceBlock<AppendEventsResponse>.LinkTo(ITargetBlock<AppendEventsResponse> target, DataflowLinkOptions linkOptions) => _executeBlock.LinkTo(target, linkOptions);
    void ISourceBlock<AppendEventsResponse>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<AppendEventsResponse> target) => ((ISourceBlock<AppendEventsResponse>)_executeBlock).ReleaseReservation(messageHeader, target);
    bool ISourceBlock<AppendEventsResponse>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<AppendEventsResponse> target) => ((ISourceBlock<AppendEventsResponse>)_executeBlock).ReserveMessage(messageHeader, target);
    void IDataflowBlock.Complete()
    {
        _appendEventBlock.Complete();
        _appendEventsBlock.Complete();
    }

    void IDataflowBlock.Fault(Exception exception)
    {
        ((IDataflowBlock)_appendEventBlock).Fault(exception);
        ((IDataflowBlock)_appendEventsBlock).Fault(exception);
        ((IDataflowBlock)_executeBlock).Fault(exception);
    }

    DataflowMessageStatus ITargetBlock<AppendEventRequest>.OfferMessage(DataflowMessageHeader messageHeader, AppendEventRequest messageValue, ISourceBlock<AppendEventRequest>? source, bool consumeToAccept) => ((ITargetBlock<AppendEventRequest>)_appendEventBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    DataflowMessageStatus ITargetBlock<AppendEventsRequest>.OfferMessage(DataflowMessageHeader messageHeader, AppendEventsRequest messageValue, ISourceBlock<AppendEventsRequest>? source, bool consumeToAccept) => ((ITargetBlock<AppendEventsRequest>)_appendEventsBlock).OfferMessage(messageHeader, messageValue, source, consumeToAccept);

    sealed record LocalContext(TaskCompletionSource<AppendResult> tcs);
}
