namespace Filin.Internal;

/// <summary>
/// A least-recently-used cache stored like a dictionary.
/// </summary>
/// <typeparam name="TKey">The type of the key to the cached item.</typeparam>
/// <typeparam name="TValue">The type of the cached item.</typeparam>
/// <param name="capacity">Maximum number of elements to cache.</param>
/// <param name="ttl">Time to live.</param>
sealed class LRUCache<TKey, TValue>(int capacity, TimeSpan ttl)
    where TKey : IEquatable<TKey>
    where TValue : notnull
{
    readonly Lock _lockObj = new();
    readonly Dictionary<TKey, Entry> _cacheMap = [with(capacity: capacity)];
    readonly LinkedList<TKey> _cacheList = new();

    public TValue GetOrUpdate(TKey key, Func<TKey, TValue> addValue, Func<TKey, TValue, TValue> updateValue)
    {
        lock (_lockObj)
        {
            if (_cacheMap.TryGetValue(key, out var entry))
            {
                if ((DateTime.Now - entry.DateAdded).Ticks <= ttl.Ticks)
                {
                    entry.Value = updateValue(key, entry.Value);

                    Touch(entry.Node);
                    return entry.Value;
                }
            }
            else
            {
                entry.Node = CreateNode(key);
            }

            var value = addValue(key);
            entry.Value = value;
            entry.DateAdded = DateTime.Now;

            _cacheMap[key] = entry;

            return value;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the cache.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Set(TKey key, TValue value)
    {
        lock (_lockObj)
        {
            if (_cacheMap.TryGetValue(key, out var entry))
            {
                entry.Value = value;
                entry.DateAdded = DateTime.Now;
                _cacheMap[key] = entry;
                Touch(entry.Node);

                return;
            }

            entry.Node = CreateNode(key);
            entry.Value = value;
            entry.DateAdded = DateTime.Now;

            _cacheMap[key] = entry;
        }
    }

    LinkedListNode<TKey> CreateNode(TKey key)
    {
        LinkedListNode<TKey> node;
        if (_cacheMap.Count >= capacity)
        {
            node = _cacheList.Last!;
            _ = _cacheMap.Remove(node.Value);
            _cacheList.RemoveLast();
            node.Value = key;
        }
        else
        {
            node = new LinkedListNode<TKey>(key);
        }

        _cacheList.AddFirst(node);
        return node;
    }

    void Touch(LinkedListNode<TKey> node)
    {
        if (node != _cacheList.First)
        {
            _cacheList.Remove(node);
            _cacheList.AddFirst(node);
        }
    }

    struct Entry(LinkedListNode<TKey> node, TValue value, DateTime dateAdded)
    {
        internal LinkedListNode<TKey> Node = node;
        internal TValue Value = value;
        internal DateTime DateAdded = dateAdded;
    }
}
