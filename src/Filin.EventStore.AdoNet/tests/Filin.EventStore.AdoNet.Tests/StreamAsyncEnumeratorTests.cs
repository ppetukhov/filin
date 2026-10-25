using System.Threading.Channels;

using Filin.EventStore.AdoNet.Tests.Internal;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public class StreamAsyncEnumeratorTests
{
    private static DcbEventSequenced CreateEvent(ulong position, string eventType = "TestEvent")
    {
        return new DcbEventSequenced(
            new EventPosition(position),
            DateTimeOffset.UtcNow,
            new DcbEvent
            {
                EventType = new EventType(eventType),
                Payload = new { Position = position }
            }
        );
    }

    [Fact]
    public async Task MoveNextAsync_ReadsFromStoreEnumeratorFirst_ThenDisposesItWhenDepleted()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10);
        var evt1 = CreateEvent(11);
        var evt2 = CreateEvent(12);

        var storeEnumerator = new TestAsyncEnumerator([evt1, evt2]);
        var query = default(DcbQuery);

        await using var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            query,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator,
            onDispose: null,
            CancellationToken.None
        );

        // Act & Assert 1
        Assert.Equal(initialEvent, enumerator.Current);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(evt1, enumerator.Current);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(evt2, enumerator.Current);

        // Act 2
        var evt3 = CreateEvent(13);
        await channel.Writer.WriteAsync(evt3);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(evt3, enumerator.Current);

        // Assert 3
        Assert.True(storeEnumerator.IsDisposed);
    }

    [Fact]
    public async Task MoveNextAsync_SkipsChannelEvents_WithPositionLessThanOrEqualToCurrent()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10);
        var query = default(DcbQuery);

        await using var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            query,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator: null,
            onDispose: null,
            CancellationToken.None
        );

        await channel.Writer.WriteAsync(CreateEvent(8));
        await channel.Writer.WriteAsync(CreateEvent(10));
        await channel.Writer.WriteAsync(CreateEvent(15));

        // Act
        var hasNext = await enumerator.MoveNextAsync();

        // Assert
        Assert.True(hasNext);
        Assert.Equal(15UL, enumerator.Current.Position.Value);
    }

    [Fact]
    public async Task CanAdd_ValidatesActivityPositionAndQueryMatch()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10, "OrderPlaced");
        var query = new DcbQuery(new DcbQueryItem(new EventType("OrderPlaced")));

        await using var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            query,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator: null,
            onDispose: null,
            CancellationToken.None
        );

        // Assert 1
        var validEvent = CreateEvent(11, "OrderPlaced");
        Assert.True(enumerator.CanAdd(validEvent));

        // Assert 2
        var oldPositionEvent = CreateEvent(10, "OrderPlaced");
        Assert.False(enumerator.CanAdd(oldPositionEvent));

        // Assert 3
        var nonMatchingEvent = CreateEvent(12, "UserCreated");
        Assert.False(enumerator.CanAdd(nonMatchingEvent));
    }

    [Fact]
    public async Task AddUnsafe_WritesToChannel_OnlyWhenValid()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10);
        var query = default(DcbQuery);

        await using var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            query,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator: null,
            onDispose: null,
            CancellationToken.None
        );

        var invalidEvent = CreateEvent(5);
        var validEvent = CreateEvent(12);

        // Act
        await enumerator.AddUnsafe(invalidEvent);
        await enumerator.AddUnsafe(validEvent);

        // Assert
        Assert.True(channel.Reader.TryRead(out var readEvent));
        Assert.Equal(12UL, readEvent.Position.Value);
        Assert.False(channel.Reader.TryRead(out _));
    }

    [Fact]
    public async Task DisposeAsync_DisposesStoreEnumeratorExecutesCallbackAndDeactivatesStream()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10);
        var storeEnumerator = new TestAsyncEnumerator([]);

        var onDisposeCalledCount = 0;
        Func<EventStore.StreamAsyncEnumerator, ValueTask> onDispose = _ =>
        {
            Interlocked.Increment(ref onDisposeCalledCount);
            return ValueTask.CompletedTask;
        };

        var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            default,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator,
            onDispose,
            CancellationToken.None
        );

        Assert.True(enumerator.IsActive);

        // Act
        await enumerator.DisposeAsync();

        // Assert
        Assert.False(enumerator.IsActive);
        Assert.True(storeEnumerator.IsDisposed);
        Assert.Equal(1, onDisposeCalledCount);

        // Act 2
        await enumerator.DisposeAsync();
        Assert.Equal(1, onDisposeCalledCount);

        // Assert 3
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task MoveNextAsync_WhenCancellationTokenCancelled_ReturnsFalse()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var channel = Channel.CreateUnbounded<DcbEventSequenced>();
        var initialEvent = CreateEvent(10);

        var enumerator = new EventStore.StreamAsyncEnumerator(
            channel,
            default,
            initialEvent,
            NullLogger.Instance,
            storeEnumerator: null,
            onDispose: null,
            cts.Token
        );

        cts.Cancel();

        // Act
        var result = await enumerator.MoveNextAsync();

        // Assert
        Assert.False(result);
        Assert.False(enumerator.IsActive);
    }
}
