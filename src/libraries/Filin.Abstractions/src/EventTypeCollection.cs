using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Represents a guaranteed pre-sorted, immutable collection of event types.
/// </summary>
[CollectionBuilder(typeof(EventTypeCollection), nameof(Create))]
public readonly struct EventTypeCollection : IEquatable<EventTypeCollection>, IComparable<EventTypeCollection>
{
    readonly EventType[]? _eventTypes;

    [MemberNotNullWhen(false, nameof(_eventTypes))]
    public bool IsEmpty => _eventTypes is not { Length: > 0 };

    public int Count => _eventTypes?.Length ?? 0;

    public EventType this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _eventTypes![index];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeCollection"/> struct by copying, 
    /// sorting, and deduplicating event types from a stack-allocated or heap-allocated span.
    /// </summary>
    /// <param name="eventTypes">The span of event types to initialize the collection with.</param>
    public EventTypeCollection(params ReadOnlySpan<EventType> eventTypes)
    {
        if (eventTypes.IsEmpty)
        {
            return;
        }

        if (eventTypes.Length == 1)
        {
            _eventTypes = [eventTypes[0]];
            return;
        }

        _eventTypes = ToSortedSet(eventTypes.ToArray());
    }

    /// <summary>
    /// Factory method utilized automatically by the C# compiler to construct the type from collection expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventTypeCollection Create(ReadOnlySpan<EventType> eventTypes) => new(eventTypes);

    /// <summary>
    /// Determines whether the collection contains a specific event type using high-performance binary search.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(EventType eventType)
    {
        if (IsEmpty) return false;
        return AsSpan().BinarySearch(eventType) >= 0;
    }

    public ReadOnlySpan<EventType> AsSpan() => _eventTypes.AsSpan();

    public Enumerator GetEnumerator() => new(_eventTypes);

    public bool Equals(EventTypeCollection other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(EventTypeCollection other) => AsSpan().SequenceCompareTo(other.AsSpan());

    public override bool Equals(object? obj) => obj is EventTypeCollection collection && Equals(collection);

    public override int GetHashCode() => IsEmpty ? 0 : CombineHashCode(_eventTypes);

    static int CombineHashCode(ReadOnlySpan<EventType> _eventTypes)
    {
        Debug.Assert(!_eventTypes.IsEmpty);

        return _eventTypes.Length switch
        {
            1 => _eventTypes[0].GetHashCode(),
            2 => HashCode.Combine(_eventTypes[0], _eventTypes[1]),
            3 => HashCode.Combine(_eventTypes[0], _eventTypes[1], _eventTypes[2]),
            4 => HashCode.Combine(_eventTypes[0], _eventTypes[1], _eventTypes[2], _eventTypes[3]),
            5 => HashCode.Combine(_eventTypes[0], _eventTypes[1], _eventTypes[2], _eventTypes[3], _eventTypes[4]),
            _ => HashCode.Combine(_eventTypes[0], _eventTypes[1], _eventTypes[2], _eventTypes[3], _eventTypes[4], CombineHashCode(_eventTypes[5..])),
        };
    }

    static EventType[] ToSortedSet(EventType[] array)
    {
        Array.Sort(array);

        int index = 1;
        for (int i = 1; i < array.Length; i++)
        {
            if (!array[i].Equals(array[i - 1]))
            {
                if (index != i)
                {
                    array[index] = array[i];
                }
                index++;
            }
        }

        if (index != array.Length)
        {
            Array.Resize(ref array, index);
        }

        return array;
    }

    /// <summary>
    /// Compares two <see cref="EventTypeCollection"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EventTypeCollection left, EventTypeCollection right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="EventTypeCollection"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EventTypeCollection left, EventTypeCollection right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the first <see cref="EventTypeCollection"/> instance precedes the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(EventTypeCollection left, EventTypeCollection right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first <see cref="EventTypeCollection"/> instance precedes or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(EventTypeCollection left, EventTypeCollection right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the first <see cref="EventTypeCollection"/> instance follows the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(EventTypeCollection left, EventTypeCollection right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first <see cref="EventTypeCollection"/> instance follows or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(EventTypeCollection left, EventTypeCollection right) => left.CompareTo(right) >= 0;

    public ref struct Enumerator
    {
        int _index;
        readonly ReadOnlySpan<EventType> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<EventType> values)
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

        public readonly EventType Current
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
            throw new InvalidOperationException("EventTypeCollection.Enumerator.Current value has not been set.");
    }
}
