using System.Threading.Tasks.Dataflow;

using Filin.EventStore.AdoNet.Internal;
using Filin.Storage;
using Filin.Storage.DataFlow;

using Moq;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public class AppendEventsBlockWrapperTests
{
    private readonly Mock<IAppendEventsBlock> _appendEventsBlockMock;
    private readonly Mock<IDisposable> _linkDisposableMock;

    public AppendEventsBlockWrapperTests()
    {
        _appendEventsBlockMock = new Mock<IAppendEventsBlock>();
        _linkDisposableMock = new Mock<IDisposable>();

        _appendEventsBlockMock
            .Setup(b => b.LinkTo(
                It.IsAny<ITargetBlock<AppendEventsResponse>>(),
                It.IsAny<DataflowLinkOptions>()))
            .Returns(_linkDisposableMock.Object);
    }

    [Fact]
    public void Constructor_WhenAppendEventsBlockIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AppendEventsBlockWrapper(null!));
    }

    [Fact]
    public void Constructor_WhenValid_CallsLinkTo()
    {
        // Act
        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);

        _appendEventsBlockMock.Verify(
            b => b.LinkTo(
                It.IsAny<ITargetBlock<AppendEventsResponse>>(),
                It.IsAny<DataflowLinkOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendAventAsync_WhenPostFails_ThrowsEventStoreException()
    {
        _appendEventsBlockMock
            .Setup(b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventRequest>(),
                It.IsAny<ISourceBlock<AppendEventRequest>>(),
                It.IsAny<bool>()))
            .Returns(DataflowMessageStatus.Declined);

        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);
        var @event = new DcbEvent
        {
            EventType = new EventType("TestEvent"),
            Tags = Tags.Create([new Tag("T1"), new Tag("T2"), new Tag("T3")]),
            Payload = new object()
        };
        var condition = new AppendCondition();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EventStoreException>(() =>
            wrapper.AppendAventAsync(@event, condition, CancellationToken.None));

        Assert.Equal("System is too busy.", ex.Message);
    }

    [Fact]
    public void AppendAventAsync_WhenPostSucceeds_ReturnsUncompletedTask()
    {
        _appendEventsBlockMock
            .Setup(b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventRequest>(),
                It.IsAny<ISourceBlock<AppendEventRequest>>(),
                It.IsAny<bool>()))
            .Returns(DataflowMessageStatus.Accepted);

        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);
        var @event = new DcbEvent
        {
            EventType = new EventType("TestEvent"),
            Tags = Tags.Create([new Tag("T1"), new Tag("T2"), new Tag("T3")]),
            Payload = new object()
        };
        var condition = new AppendCondition();

        // Act
        var task = wrapper.AppendAventAsync(@event, condition, CancellationToken.None);

        // Assert
        Assert.False(task.IsCompleted);
        _appendEventsBlockMock.Verify(
            b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventRequest>(),
                It.IsAny<ISourceBlock<AppendEventRequest>>(),
                It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendEventsAsync_WhenPostFails_ThrowsEventStoreException()
    {
        // Arrange
        _appendEventsBlockMock
            .Setup(b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventsRequest>(),
                It.IsAny<ISourceBlock<AppendEventsRequest>>(),
                It.IsAny<bool>()))
            .Returns(DataflowMessageStatus.Declined);

        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);
        var events = new List<DcbEvent> { new DcbEvent
        {
            EventType = new EventType("TestEvent"),
            Tags = Tags.Create([new Tag("T1"), new Tag("T2"), new Tag("T3")]),
            Payload = new object()
        }};
        var condition = new AppendCondition();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EventStoreException>(() =>
            wrapper.AppendEventsAsync(events, condition, CancellationToken.None));

        Assert.Equal("System is too busy.", ex.Message);
    }

    [Fact]
    public void AppendEventsAsync_WhenPostSucceeds_ReturnsUncompletedTask()
    {
        // Arrange
        _appendEventsBlockMock
            .Setup(b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventsRequest>(),
                It.IsAny<ISourceBlock<AppendEventsRequest>>(),
                It.IsAny<bool>()))
            .Returns(DataflowMessageStatus.Accepted);

        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);
        var events = new List<DcbEvent> { new DcbEvent
        {
            EventType = new EventType("TestEvent"),
            Tags = Tags.Create([new Tag("T1"), new Tag("T2"), new Tag("T3")]),
            Payload = new object()
        }};
        var condition = new AppendCondition();

        // Act
        var task = wrapper.AppendEventsAsync(events, condition, CancellationToken.None);

        // Assert
        Assert.False(task.IsCompleted);
        _appendEventsBlockMock.Verify(
            b => b.OfferMessage(
                It.IsAny<DataflowMessageHeader>(),
                It.IsAny<AppendEventsRequest>(),
                It.IsAny<ISourceBlock<AppendEventsRequest>>(),
                It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingLink()
    {
        // Arrange
        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);

        // Act
        wrapper.Dispose();

        // Assert
        _linkDisposableMock.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        // Arrange
        var wrapper = new AppendEventsBlockWrapper(_appendEventsBlockMock.Object);

        // Act
        wrapper.Dispose();
        wrapper.Dispose();

        // Assert
        _linkDisposableMock.Verify(d => d.Dispose(), Times.Once);
    }
}
