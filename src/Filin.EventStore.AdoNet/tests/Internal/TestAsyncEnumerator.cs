namespace Filin.EventStore.AdoNet.Tests.Internal;

sealed class TestAsyncEnumerator(IEnumerable<DcbEventSequenced> items) : IAsyncEnumerator<DcbEventSequenced>
{
    readonly Queue<DcbEventSequenced> _items = new(items);
    public bool IsDisposed { get; private set; }

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
