using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Internal;

namespace Filin;

/// <summary>
/// Represents a guaranteed pre-sorted, immutable collection of <see cref="DcbQueryItem"/> instances.
/// </summary>
[CollectionBuilder(typeof(DcbQuery), nameof(Create))]
[DebuggerTypeProxy(typeof(DcbQueryDebugView))]
[DebuggerDisplay("{Internal.DcbQueryDebugView.GetDisplayString(this),nq}")]
public readonly struct DcbQuery : IEquatable<DcbQuery>, IComparable<DcbQuery>
{
    readonly DcbQueryItem[]? _items;

    /// <summary>
    /// Gets a value indicating whether the current collection is empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_items))]
    public bool IsEmpty => _items is not { Length: > 0 };

    /// <summary>
    /// Gets the total number of elements in the collection.
    /// </summary>
    public int Count => _items?.Length ?? 0;

    /// <summary>
    /// Gets the <see cref="DcbQueryItem"/> at the specified index.
    /// </summary>
    public DcbQueryItem this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _items![index];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DcbQuery"/> struct by copying, 
    /// sorting, and deduplicating items from a stack-allocated or heap-allocated span.
    /// </summary>
    /// <param name="items">The span of query items to initialize the collection with.</param>
    public DcbQuery(params ReadOnlySpan<DcbQueryItem> items)
    {
        if (items.IsEmpty)
        {
            return;
        }

        if (items.Length == 1)
        {
            _items = [items[0]];
            return;
        }

        _items = ToSortedSet(items.ToArray());
    }

    /// <summary>
    /// Factory method utilized automatically by the C# compiler to construct the type from collection expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DcbQuery Create(ReadOnlySpan<DcbQueryItem> items) => new(items);

    /// <summary>
    /// Determines whether the collection contains a specific query item using high-performance binary search.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(DcbQueryItem item) => IsEmpty ? false : AsSpan().BinarySearch(item) >= 0;

    /// <summary>
    /// Combines the current query specification with an additional <see cref="DcbQueryItem"/> boundary block.
    /// Items targeting the same tag boundaries are merged automatically.
    /// </summary>
    /// <param name="queryItem">The query item constraint to append.</param>
    /// <returns>A new <see cref="DcbQuery"/> instance containing the combined, optimized boundary specifications.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DcbQuery Or(DcbQueryItem queryItem)
    {
        if (IsEmpty) return [queryItem];

        return [.. this, queryItem];
    }

    /// <summary>
    /// Combines the current query specification with another composite <see cref="DcbQuery"/> block.
    /// Items targeting identical tag boundaries are merged automatically and sorted deterministically.
    /// </summary>
    /// <param name="query">The other composite query specification to merge with.</param>
    /// <returns>A new <see cref="DcbQuery"/> instance representing the union of both specifications.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DcbQuery Or(DcbQuery query)
    {
        if (IsEmpty) return query;
        if (query.IsEmpty) return this;

        return [.. this, .. query];
    }

    /// <summary>
    /// Determines whether the specified domain event matches any of the consistency constraints 
    /// defined within this query specification.
    /// </summary>
    /// <param name="event">The domain event container to evaluate.</param>
    /// <returns><see langword="true"/> if the event satisfies at least one query item boundary; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool MatchesEvent(DcbEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (IsEmpty)
        {
            return true;
        }

        var itemsSpan = _items.AsSpan();
        for (int i = 0; i < itemsSpan.Length; i++)
        {
            if (itemsSpan[i].MatchesEvent(@event))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a new span over the collection.
    /// </summary>
    public ReadOnlySpan<DcbQueryItem> AsSpan() => _items.AsSpan();

    /// <summary>
    /// Returns a high-performance stack-only enumerator over the collection.
    /// </summary>
    public Enumerator GetEnumerator() => new(_items);

    public bool Equals(DcbQuery other) => AsSpan().SequenceEqual(other.AsSpan());

    public override bool Equals(object? obj) => obj is DcbQuery collection && Equals(collection);

    public override int GetHashCode() => IsEmpty ? 0 : CombineHashCode(_items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DcbQuery other) => AsSpan().SequenceCompareTo(other.AsSpan());

    static int CombineHashCode(ReadOnlySpan<DcbQueryItem> items)
    {
        Debug.Assert(!items.IsEmpty);

        return items.Length switch
        {
            1 => items[0].GetHashCode(),
            2 => HashCode.Combine(items[0], items[1]),
            3 => HashCode.Combine(items[0], items[1], items[2]),
            4 => HashCode.Combine(items[0], items[1], items[2], items[3]),
            5 => HashCode.Combine(items[0], items[1], items[2], items[3], items[4]),
            _ => HashCode.Combine(items[0], items[1], items[2], items[3], items[4], CombineHashCode(items[5..])),
        };
    }

    static DcbQueryItem[] ToSortedSet(DcbQueryItem[] array)
    {
        // 1. Sort items by tags first to group identical boundaries together
        Array.Sort(array, CompareItemsByTags);

        int index = 1;
        for (int i = 1; i < array.Length; i++)
        {
            // Check if the current item has the exact same tag boundary as the previous unique item
            if (array[i].Tags.Equals(array[index - 1].Tags))
            {
                // This invokes CollectionBuilder under the hood, ensuring automatic sorting and deduplication.
                EventTypeCollection mergedTypes = [.. array[index - 1].EventTypes, .. array[i].EventTypes];

                // Reconstruct the immutable query item with the combined event types
                array[index - 1] = new DcbQueryItem(mergedTypes, array[index - 1].Tags);
            }
            else
            {
                // Tags are different, shift the independent boundary forward
                if (index != i)
                {
                    array[index] = array[i];
                }
                index++;
            }
        }

        // 2. Shrink the array to contain only unique tag groups
        if (index != array.Length)
        {
            Array.Resize(ref array, index);
        }

        // 3. Final touch: Sort the merged array by its default IComparable specification 
        // to maintain deterministic lexicographical order across the entire collection.
        Array.Sort(array);

        return array;
    }

    /// <summary>
    /// A high-performance, allocation-free static comparison method used to group query items strictly by their tag boundaries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CompareItemsByTags(DcbQueryItem x, DcbQueryItem y) => x.Tags.CompareTo(y.Tags);

    /// <summary>
    /// Compares two <see cref="DcbQuery"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DcbQuery left, DcbQuery right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="DcbQuery"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DcbQuery left, DcbQuery right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the first <see cref="DcbQuery"/> instance precedes the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(DcbQuery left, DcbQuery right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first <see cref="DcbQuery"/> instance precedes or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(DcbQuery left, DcbQuery right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the first <see cref="DcbQuery"/> instance follows the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(DcbQuery left, DcbQuery right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first <see cref="DcbQuery"/> instance follows or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(DcbQuery left, DcbQuery right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Provides a high-performance, stack-only enumerator over a collection.
    /// </summary>
    public ref struct Enumerator
    {
        int _index;
        readonly ReadOnlySpan<DcbQueryItem> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<DcbQueryItem> values)
        {
            _span = values;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int nextIndex = _index + 1;
            if (nextIndex < _span.Length)
            {
                _index = nextIndex;
                return true;
            }

            _index = nextIndex;
            return false;
        }

        public readonly DcbQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index < 0 || _index >= _span.Length)
                {
                    ThrowInvalidOperationException();
                }
                return _span[_index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        static void ThrowInvalidOperationException() =>
            throw new InvalidOperationException("DcbQuery.Enumerator.Current value has not been set.");
    }
}
