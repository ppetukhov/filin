using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

using Filin.Configuration;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Filin.EventStore.Postgres.DataFlow;

sealed class AppendEventsBlock : IAppendEventsBlock, IDisposable
{
    readonly record struct ExecuteRequest(Func<DbCommand, object, CancellationToken, Task<AppendResult>> Handler, DbCommand Command, object State, IContext Context);

    static readonly TimeSpan[] RetryDelays = [.. Enumerable.Range(1, 15).Select(i => double.Pow(2.0, i) * TimeSpan.FromMilliseconds(3))];

    readonly TransformBlock<AppendEventRequest, ExecuteRequest> _appendEventBlock;
    readonly TransformBlock<AppendEventsRequest, ExecuteRequest> _appendEventsBlock;
    readonly TransformBlock<ExecuteRequest, AppendEventsResponse> _executeBlock;
    readonly ILogger _logger;
    readonly List<IDisposable> _disposable;
    bool _disposed;

    public Task Completion => _executeBlock.Completion;

    public AppendEventsBlock(DbCommandFactory commandFactory, IOptions<EventStorePostgresOptions> options, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres");

        var execOpt = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BoundedCapacity = options.Value.AppendBoundedCapacity,
            EnsureOrdered = false
        };

        _appendEventBlock = new(request =>
        {
            var (@event, appendCondition, context) = request;

            var command = commandFactory.CreateAppendEventCommand(@event, appendCondition);

            return new ExecuteRequest { Handler = AppendEventInner, Command = command, State = @event, Context = context };
        }, execOpt);

        _appendEventsBlock = new(request =>
        {
            var (events, appendCondition, context) = request;

            var command = commandFactory.CreateAppendEventsCommand(events, appendCondition);

            return new ExecuteRequest { Handler = AppendEventsInner, Command = command, State = events, Context = context };
        }, execOpt);

        _executeBlock = new(ExecuteAsync,
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                EnsureOrdered = false
            });

        var linkOption = new DataflowLinkOptions { PropagateCompletion = true };

        var execSingleOpt = new ExecutionDataflowBlockOptions
        {
            EnsureOrdered = false,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            SingleProducerConstrained = true
        };

        _disposable =
        [
            _appendEventBlock.LinkTo(_executeBlock, linkOption),
            _appendEventsBlock.LinkTo(_executeBlock, linkOption),
        ];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var item in _disposable)
        {
            item.Dispose();
        }

        _disposable.Clear();

        _disposed = true;
    }

    async Task<AppendResult> AppendEventInner(DbCommand command, object state, CancellationToken cancellationToken)
    {
        var @event = (DcbEvent)state;

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

    async Task<AppendEventsResponse> ExecuteAsync(ExecuteRequest request)
    {
        var (handler, command, state, context) = request;

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
                        await Task.Delay(delay, context.CancellationToken).ConfigureAwait(true);
                    }

                    var result = await handler(command, state, context.CancellationToken).ConfigureAwait(true);

                    return new AppendEventsResponse(result, context);
                }
                catch (DbException e) when (e.IsTransient)
                {
                    if (retry < RetryDelays.Length)
                    {
                        delay = RetryDelays[retry];
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

                    _ = context.TrySetCanceled(ex.CancellationToken);

                    return new AppendEventsResponse(AppendResult.Empty, context);
                }
#pragma warning disable CA1031 // Modify 'ExecuteAsync' to catch a more specific allowed exception type, or rethrow the exception
                catch (Exception ex)
#pragma warning restore CA1031 // Modify 'ExecuteAsync' to catch a more specific allowed exception type, or rethrow the exception
                {
                    _logger.ErrorAppendEvents(ex);

                    _ = context.TrySetException(ex);

                    return new AppendEventsResponse(AppendResult.Empty, context);
                }
            }
        }
    }

    [DoesNotReturn]
    static void ThrowUnexpectedRowsCount(int expectedCount, int count)
    {
        var ex = new EventStoreException("Unexpected rows count");

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
}
