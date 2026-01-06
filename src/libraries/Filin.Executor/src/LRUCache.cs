using System.Diagnostics.CodeAnalysis;

namespace Filin;

/// <summary>
/// A least-recently-used cache stored like a dictionary.
/// </summary>
/// <typeparam name="TKey">The type of the key to the cached item.</typeparam>
/// <typeparam name="TValue">The type of the cached item.</typeparam>
/// <remarks>
/// Code was taken from Microsoft Bot Framework SDK: https://github.com/microsoft/botbuilder-dotnet.
/// Class link: https://github.com/microsoft/botbuilder-dotnet/blob/402bc02b4cbbd2f4ec359134640e99211367e4a5/libraries/AdaptiveExpressions/LRUCache.cs
/// </remarks>
/// <param name="capacity">Maximum number of elements to cache.</param>
/// <param name="ttl">Time to live.</param>
sealed class LRUCache<TKey, TValue>(int capacity, TimeSpan ttl)
    where TKey : IEquatable<TKey>
    where TValue : class
{
    readonly Lock _lockObj = new Lock();
    readonly Dictionary<TKey, Entry> _cacheMap = new Dictionary<TKey, Entry>(capacity: capacity);
    readonly LinkedList<TKey> _cacheList = new LinkedList<TKey>();

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with
    /// the specified key, if the key is found; otherwise, the default value for the 
    /// type of the <paramref name="value" /> parameter.</param>
    /// <returns>true if contains an element with the specified key; otherwise, false.</returns>
    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (_lockObj)
        {
            if (_cacheMap.TryGetValue(key, out var entry))
            {
                if ((DateTime.Now - entry.DateAdded).Ticks > ttl.Ticks)
                {
                    _cacheList.Remove(entry.Node);
                    _cacheMap.Remove(key);

                    value = default;
                    return false;
                }

                Touch(entry.Node);
                value = entry.Value;
                return true;
            }
        }

        value = default;
        return false;
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
            if (!_cacheMap.TryGetValue(key, out var entry))
            {
                LinkedListNode<TKey> node;
                if (_cacheMap.Count >= capacity)
                {
                    node = _cacheList.Last!;
                    _cacheMap.Remove(node.Value);
                    _cacheList.RemoveLast();
                    node.Value = key;
                }
                else
                {
                    node = new LinkedListNode<TKey>(key);
                }

                _cacheList.AddFirst(node);
                _cacheMap.Add(key, new Entry(node, value, DateTime.Now));
            }
            else
            {
                entry.Value = value;
                _cacheMap[key] = entry;
                Touch(entry.Node);
            }
        }
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
