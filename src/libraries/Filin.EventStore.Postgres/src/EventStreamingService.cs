using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Channels;

using Filin.Configuration;
using Filin.Storage;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Filin.EventStore.Postgres;

sealed class EventStreamingService(
    Channel<Message> channel,
    IEventStore eventStore,
    IOptions<EventStorePostgresOptions> options,
    ILoggerFactory loggerFactory) : BackgroundService
{
    readonly ILogger _logger = loggerFactory.CreateLogger("Filin.EventStore.Postgres.Streaming");
    readonly LinkedList<WeakReference<EventStore.StreamAsyncEnumerator>> _weakConsumers = new();

    (QueryStatus Status, DcbQuery Query) _query = (QueryStatus.None, default);
    EventPosition _lastEventPosition;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.StreamingServiceStarted();

        try
        {
            await Task.WhenAll(ListenPostgresNotificationsAsync(stoppingToken), ProcessMessagesAsync(stoppingToken)).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.StreamingServiceError(e);
        }
        finally
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _ = channel.Writer.TryComplete();
            }

            _logger.StreamingServiceStopped();
        }

        // Initializes the Postgres Listener by issueing a LISTEN Command.
        async Task ListenPostgresNotificationsAsync(CancellationToken stoppingToken)
        {
            // Open a new Connection, which can be used to issue the LISTEN Command 
            // to the Postgres Database.
            using var connection = new NpgsqlConnection(options.Value.ConnectionString);

            connection.Notification += (_, args) =>
            {
                try
                {
                    var pgEventPayload = JsonSerializer.Deserialize(args.Payload, StoreJsonSerializerContext.Default.PgEventPayload)
                        ?? throw FailedDeserializePayload(args.Payload, args.PID, args.Channel);

                    var msg = Message.Event(pgEventPayload.ToEventSequenced());

                    while (!channel.Writer.TryWrite(msg))
                    {
                        Thread.SpinWait(10);
                        stoppingToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.StreamingServiceEventProcessingError(e);
                }
            };

            var connectToDatabase = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true, SingleWriter = true });

            connection.StateChange += (_, args) =>
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.EventStoreConnectionStateChanged(args.OriginalState, args.CurrentState);
                }

                if (args.CurrentState == ConnectionState.Closed)
                {
                    _ = connectToDatabase.Writer.TryWrite(true);
                }
            };

            _ = connectToDatabase.Writer.TryWrite(true);

            await foreach (var item in connectToDatabase.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                if (connection.State == ConnectionState.Open)
                {
                    // Already LISTEN postgres events
                    return;
                }

                await connection.ConnectToEventStoreAsync(options.Value.Backoff, _logger, stoppingToken).ConfigureAwait(false);

                try
                {
                    // We register to the Notifications on the Channel.
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"LISTEN dcb_events";
                        command.Connection = connection;

                        _ = await command
                            .ExecuteNonQueryAsync(stoppingToken)
                            .ConfigureAwait(false);
                    }

                    // And now we are putting the Connection into the Wait State,
                    // until the Cancellation is requested.
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await connection.WaitAsync(stoppingToken).ConfigureAwait(false);
                    }
                }
                catch (DbException ex)
                {
                    _logger.StreamingServiceListenPostgresEventsDbError(ex, ex.SqlState, ex.ErrorCode);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.StreamingServiceListenPostgresEventsError(e);
                }
            }
        }

        static InvalidDataException FailedDeserializePayload(string payload, int pid, string channel)
        {
            var ex = new InvalidDataException($"Failed to deserialize {nameof(PgEventPayload)}");
            ex.Data["Payload"] = payload;
            ex.Data["PostgrePID"] = pid;
            ex.Data["Channel"] = channel;
            return ex;
        }

        async Task ProcessMessagesAsync(CancellationToken stoppingToken)
        {
            await foreach (var streamData in channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                if (streamData.EventSequenced is DcbEventSequenced eventSequenced)
                {
                    await ProcessEvent(eventSequenced, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                switch (streamData.Command)
                {
                    case Message.SubscribeCommand subscribe:
                        Subscribe(subscribe);
                        break;
                    case Message.UnsubscribeCommand unsubscribe:
                        Unsubscribe(unsubscribe);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected command {streamData.Command?.GetType().FullName ?? "null"}");
                }
            }
        }

        async ValueTask ProcessEvent(DcbEventSequenced partialEvent, CancellationToken cancellationToken)
        {
            // fast path
            if (_query.Status == QueryStatus.None
                || partialEvent.Position <= _lastEventPosition
                || (_query.Status != QueryStatus.NotActual && !_query.Query.MatchesEvent(partialEvent.Event)))
            {
                return;
            }

            // actualize query
            switch (_query.Status)
            {
                case QueryStatus.Actual:
                    break;
                case QueryStatus.NotActual:
                    var builder = new List<DcbQueryItem>(capacity: _weakConsumers.Count);

                    var hasActiveConsumer = false;
                    var allowAnyEvent = false;

                    var node = _weakConsumers.First;

                    while (node is not null)
                    {
                        if (node.Value.TryGetTarget(out var consumer) && consumer.IsActive)
                        {
                            hasActiveConsumer = true;

                            foreach (var item in consumer.Query)
                            {
                                if (item.IsEmpty)
                                {
                                    allowAnyEvent = true;
                                    break;
                                }
                                builder.Add(item);
                            }
                        }
                        else
                        {
                            _weakConsumers.Remove(node);
                        }

                        if (allowAnyEvent)
                        {
                            break;
                        }

                        node = node.Next;
                    }

                    // fast path
                    if (!hasActiveConsumer)
                    {
                        _query = (QueryStatus.None, default);
                        return;
                    }

                    // fast path
                    if (allowAnyEvent)
                    {
                        _query = (QueryStatus.Actual, default);
                    }
                    else
                    {
                        _query = (QueryStatus.Actual, new DcbQuery([.. builder]));

                        if (!_query.Query.MatchesEvent(partialEvent.Event))
                        {
                            return;
                        }
                    }
                    break;
                case QueryStatus.None:
                    return;
                default:
                    break;
            }

            var afterEventPosition = new EventPosition(partialEvent.Position.Value - 1);

            await foreach (var @event in eventStore.ReadAsync(_query.Query, afterEventPosition).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var node = _weakConsumers.First;

                var dirty = false;

                while (node is not null)
                {
                    if (node.Value.TryGetTarget(out var consumer) && consumer.IsActive)
                    {
                        if (consumer.CanAdd(@event))
                        {
                            await consumer.UnsafeAddEvent(@event).ConfigureAwait(false);

                            _lastEventPosition = @event.Position;
                        }
                    }
                    else
                    {
                        _weakConsumers.Remove(node);
                        dirty = true;
                    }

                    node = node.Next;
                }

                if (dirty)
                {
                    MarkQueryAsNotActual();
                }
            }
        }
    }

    void Subscribe(Message.SubscribeCommand subscribe)
    {
        if (!subscribe.Subscriber.TryGetTarget(out var subscriber) || !subscriber.IsActive)
        {
            return;
        }

        _ = _weakConsumers.AddLast(subscribe.Subscriber);

        switch (_query.Status)
        {
            case QueryStatus.Actual:
                _query = (QueryStatus.Actual, _query.Query.Or(subscriber.Query));
                break;
            case QueryStatus.None:
                _query = (QueryStatus.Actual, subscriber.Query);
                break;
            case QueryStatus.NotActual:
                break;
            default:
                break;
        }
    }

    void Unsubscribe(Message.UnsubscribeCommand unsubscribe)
    {
        var hasUnsub = unsubscribe.Subscriber.TryGetTarget(out var consumerToUnsub);

        var node = _weakConsumers.First;

        var dirty = false;

        while (node is not null)
        {
            if (node.Value.TryGetTarget(out var consumer))
            {
                if (hasUnsub && consumer == consumerToUnsub)
                {
                    _weakConsumers.Remove(node);
                    dirty = true;
                    break;
                }
            }
            else
            {
                _weakConsumers.Remove(node);
                dirty = true;
            }

            node = node.Next;
        }

        if (dirty)
        {
            MarkQueryAsNotActual();
        }
    }

    void MarkQueryAsNotActual()
    {
        _query = _weakConsumers.Count switch
        {
            0 => (QueryStatus.None, default),
            _ => (QueryStatus.NotActual, default),
        };
    }

    enum QueryStatus
    {
        None,
        NotActual,
        Actual,
    }
}
