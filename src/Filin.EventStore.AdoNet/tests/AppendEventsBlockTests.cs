using System.Threading.Tasks.Dataflow;

using Filin.Configuration;
using Filin.EventStore.AdoNet.DataFlow;
using Filin.EventStore.AdoNet.Tests.Internal;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public sealed class AppendEventsBlockTests : IDisposable
{
    readonly AppendBlockTestDbCommandFactory _commandFactory;
    readonly IOptions<EventStoreAdoNetOptions> _options;
    readonly AppendEventsBlock _block;
    readonly BufferBlock<AppendEventsResponse> _outputBuffer;

    public AppendEventsBlockTests()
    {
        _commandFactory = new AppendBlockTestDbCommandFactory();

        _options = Options.Create(new EventStoreAdoNetOptions
        {
            Backoff = new EventStoreAdoNetBackoff(),
            StorageSerializer = new TestStorageSerializer(),
            DataSource = new TestDbDataSource(),
            AppendBoundedCapacity = 100,
            MaxConcurrentAppendRequests = 4
        });

        _block = new AppendEventsBlock(_commandFactory, _options, NullLoggerFactory.Instance);

        _outputBuffer = new BufferBlock<AppendEventsResponse>();
        _ = ((ISourceBlock<AppendEventsResponse>)_block).LinkTo(_outputBuffer, new DataflowLinkOptions { PropagateCompletion = true });
    }

    [Fact]
    public async Task AppendEvent_WhenSuccess_ReturnsSequencedEventResult()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _commandFactory.CommandToReturn.ReaderToReturn = new AppendBlockTestDbDataReader([(100L, now)]);

        var @event = new DcbEvent
        {
            EventType = new EventType("UserCreated"),
            Payload = new { UserId = 1 }
        };

        var context = new AppendBlockTestContext();
        var request = new AppendEventRequest(@event, default, context);

        // Act
        var posted = _block.Post(request);
        Assert.True(posted);

        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(response.Result.IsSuccess);
        Assert.False(response.Result.IsConflict);
        Assert.Equal(1, response.Result.Count);

        var sequencedEvent = response.Result[0];
        Assert.Equal(100UL, sequencedEvent.Position.Value);
        Assert.Equal(now, sequencedEvent.Timestamp.DateTime);
        Assert.Equal("UserCreated", sequencedEvent.Event.EventType.Value);
    }

    [Fact]
    public async Task AppendEvent_WhenNoRowsReturned_ReturnsConflictResult()
    {
        // Arrange
        _commandFactory.CommandToReturn.ReaderToReturn = new AppendBlockTestDbDataReader([]);

        var @event = new DcbEvent
        {
            EventType = new EventType("OrderPlaced"),
            Payload = new { OrderId = 10 }
        };

        var context = new AppendBlockTestContext();
        var request = new AppendEventRequest(@event, default, context);

        // Act
        _ = _block.Post(request);
        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(response.Result.IsConflict);
        Assert.False(response.Result.IsSuccess);
        Assert.Equal(0, response.Result.Count);
    }

    [Fact]
    public async Task AppendEvents_BatchSuccess_ReturnsAllSequencedEvents()
    {
        // Arrange
        var t1 = DateTime.UtcNow;
        var t2 = DateTime.UtcNow.AddSeconds(1);

        _commandFactory.CommandToReturn.ReaderToReturn = new AppendBlockTestDbDataReader([
            (201L, t1),
            (202L, t2)
        ]);

        var events = new List<DcbEvent>
        {
            new() { EventType = new EventType("Item1"), Payload = "p1" },
            new() { EventType = new EventType("Item2"), Payload = "p2" }
        };

        var context = new AppendBlockTestContext();
        var request = new AppendEventsRequest(events, default, context);

        // Act
        var posted = _block.Post(request);
        Assert.True(posted);

        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(response.Result.IsSuccess);
        Assert.Equal(2, response.Result.Count);

        Assert.Equal(201UL, response.Result[0].Position.Value);
        Assert.Equal(202UL, response.Result[1].Position.Value);
    }

    [Fact]
    public async Task AppendEvents_WhenPartialRowsReturned_SetsExceptionOnContext()
    {
        // Arrange
        _commandFactory.CommandToReturn.ReaderToReturn = new AppendBlockTestDbDataReader([
            (301L, DateTime.UtcNow)
        ]);

        var events = new List<DcbEvent>
        {
            new() { EventType = new EventType("Evt1"), Payload = "p1" },
            new() { EventType = new EventType("Evt2"), Payload = "p2" }
        };

        var context = new AppendBlockTestContext();
        var request = new AppendEventsRequest(events, default, context);

        // Act
        _ = _block.Post(request);
        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(context.Exception);
        _ = Assert.IsType<EventStoreException>(context.Exception);
        Assert.Equal("Unexpected rows count", context.Exception.Message);
        Assert.True(response.Result.IsConflict || response.Result.Count == 0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationCanceled_SetsCanceledOnContext()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _commandFactory.CommandToReturn.ExceptionToThrow = new OperationCanceledException(cts.Token);

        var @event = new DcbEvent { EventType = new EventType("TestEvent"), Payload = "test" };
        var context = new AppendBlockTestContext { CancellationToken = cts.Token };
        var request = new AppendEventRequest(@event, default, context);

        // Act
        _ = _block.Post(request);
        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(context.IsCanceled);
        Assert.Equal(AppendResult.Empty, response.Result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGenericExceptionThrown_SetsExceptionOnContext()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failure");
        _commandFactory.CommandToReturn.ExceptionToThrow = expectedException;

        var @event = new DcbEvent { EventType = new EventType("TestEvent"), Payload = "test" };
        var context = new AppendBlockTestContext();
        var request = new AppendEventRequest(@event, default, context);

        // Act
        _ = _block.Post(request);
        var response = await _outputBuffer.ReceiveAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(context.Exception);
        Assert.Same(expectedException, context.Exception);
        Assert.Equal(AppendResult.Empty, response.Result);
    }

    public void Dispose() => _block.Dispose();
}
