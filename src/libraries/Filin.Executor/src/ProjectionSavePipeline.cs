using System.Threading.Channels;

namespace Filin;

sealed class ProjectionSavePipeline<T>(int capacity = 1000)
{
    readonly Channel<T> _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity: capacity)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    internal bool TryWrite(T item) => _channel.Writer.TryWrite(item);

    internal ChannelReader<T> Reader => _channel.Reader;
}
