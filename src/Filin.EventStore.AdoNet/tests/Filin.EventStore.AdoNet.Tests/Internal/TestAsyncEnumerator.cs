namespace Filin.EventStore.AdoNet.Tests.Internal;

public class TestAsyncEnumerator : IAsyncEnumerator<DcbEventSequenced>
{
    private readonly Queue<DcbEventSequenced> _items;
    public bool IsDisposed { get; private set; }

    public TestAsyncEnumerator(IEnumerable<DcbEventSequenced> items)
    {
        _items = new Queue<DcbEventSequenced>(items);
    }

    public DcbEventSequenced Current { get; private set; }

    public ValueTask<bool> MoveNextAsync()
    {
        if (_items.TryDequeue(out var item))
        {
            Current = item;
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
