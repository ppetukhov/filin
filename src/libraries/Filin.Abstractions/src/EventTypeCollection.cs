using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Internal;

namespace Filin;

/// <summary>
/// Represents a guaranteed pre-sorted, immutable collection of event types.
/// </summary>
[CollectionBuilder(typeof(EventTypeCollection), nameof(Create))]
[DebuggerTypeProxy(typeof(EventTypeCollectionDebugView))]

public readonly struct EventTypeCollection : IEquatable<EventTypeCollection>, IComparable<EventTypeCollection>, ISpanFormattable
{
    readonly EventType[]? _eventTypes;

    /// <summary>
    /// Gets a value indicating whether the collection is empty.
    /// </summary>
    /// <value><see langword="true"/> if the collection contains no elements; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(false, nameof(_eventTypes))]
    public bool IsEmpty => _eventTypes is not { Length: > 0 };

    /// <summary>
    /// Gets the total number of event types contained in the collection.
    /// </summary>
    /// <value>The count of unique event types.</value>
    public int Count => _eventTypes?.Length ?? 0;

    /// <summary>
    /// Gets the <see cref="EventType"/> at the specified index within the sorted collection.
    /// </summary>
    /// <param name="index">The zero-based index of the event type to get.</param>
    /// <returns>The <see cref="EventType"/> element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or out of bounds.</exception>
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
    /// Factory method utilized automatically by the C# compiler to construct the collection type from collection expressions.
    /// </summary>
    /// <param name="eventTypes">The span of event types supplied by the compiler or environment.</param>
    /// <returns>A new <see cref="EventTypeCollection"/> containing the sorted, unique event types.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventTypeCollection Create(ReadOnlySpan<EventType> eventTypes) => new(eventTypes);

    /// <summary>
    /// Determines whether the collection contains a specific event type using high-performance binary search.
    /// </summary>
    /// <param name="eventType">The event type to locate in the collection.</param>
    /// <returns><see langword="true"/> if the event type is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(EventType eventType) => IsEmpty ? false : AsSpan().BinarySearch(eventType) >= 0;

    /// <summary>
    /// Returns a read-only span representation of the underlying sorted event types array.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> over the elements of the collection.</returns>
    public ReadOnlySpan<EventType> AsSpan() => _eventTypes.AsSpan();

    public Enumerator GetEnumerator() => new(_eventTypes);

    public bool Equals(EventTypeCollection other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(EventTypeCollection other) => AsSpan().SequenceCompareTo(other.AsSpan());

    public override bool Equals(object? obj) => obj is EventTypeCollection collection && Equals(collection);

    public override int GetHashCode() => IsEmpty ? 0 : CombineHashCode(_eventTypes);

    /// <summary>
    /// Formats the collection of event types into the provided span of characters using standard comma-and-space separator.
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        // 1. Delegate to custom formatter if explicitly provided by the caller
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter customFormatter)
        {
            var formattedString = customFormatter.Format(format.ToString(), this, provider);
            var formattedSpan = formattedString.AsSpan();

            if (destination.Length < formattedSpan.Length)
            {
                charsWritten = 0;
                return false;
            }

            formattedSpan.CopyTo(destination);
            charsWritten = formattedSpan.Length;
            return true;
        }

        // 2. High-performance strict comma-and-space fast-path
        charsWritten = 0;
        if (IsEmpty) return true;

        var count = Count;
        var separator = ", ".AsSpan();
        var position = 0;

        for (var i = 0; i < count; i++)
        {
            var typeValueSpan = this[i].Value.AsSpan();

            if (destination[position..].Length < typeValueSpan.Length) return false;
            typeValueSpan.CopyTo(destination[position..]);
            position += typeValueSpan.Length;

            if (i < count - 1)
            {
                if (destination[position..].Length < separator.Length) return false;
                separator.CopyTo(destination[position..]);
                position += separator.Length;
            }
        }

        charsWritten = position;
        return true;
    }

    /// <summary>
    /// Formats the collection into a string layout, respecting custom formatting providers.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter customFormatter
            ? customFormatter.Format(format, this, formatProvider)
            : ToString();

    /// <summary>
    /// Returns a string that represents the current collection of event types, joined by a strict comma-and-space separator.
    /// Allocates strictly on the stack if the combined total length does not exceed 256 characters.
    /// </summary>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var count = Count;
        if (count == 1)
        {
            return this[0].Value;
        }

        // Calculate exact string buffer requirement allocation bounds
        var totalLength = 0;
        for (var i = 0; i < count; i++)
        {
            totalLength += this[i].Value.Length;
        }
        totalLength += (count - 1) * 2; // 2 chars per separator (", ")

        const int stackAllocThreshold = 256;
        if (totalLength <= stackAllocThreshold)
        {
            Span<char> buffer = stackalloc char[totalLength];
            TryFormat(buffer, out _, default, null);
            return new string(buffer);
        }

        var rentedArray = ArrayPool<char>.Shared.Rent(totalLength);
        try
        {
            Span<char> buffer = rentedArray.AsSpan(0, totalLength);
            TryFormat(buffer, out _, default, null);
            return new string(buffer);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentedArray);
        }
    }

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

    /// <summary>
    /// Enumerates the elements of an <see cref="EventTypeCollection"/> using a stack-allocated structure.
    /// </summary>
    public ref struct Enumerator
    {
        int _index;
        readonly ReadOnlySpan<EventType> _span;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct with the specified span of event types.
        /// </summary>
        /// <param name="values">The underlying span sequence to iterate over.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<EventType> values)
        {
            _span = values;
            _index = -1;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the enumerator was successfully advanced to the next element; 
        /// <see langword="false"/> if the enumerator has passed the end of the collection.
        /// </returns>
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

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The <see cref="EventType"/> element in the collection at the current position of the enumerator.</value>
        /// <exception cref="InvalidOperationException">Thrown if the enumerator is positioned before the first element or after the last element.</exception>
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
