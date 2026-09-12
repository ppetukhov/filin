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
    public readonly bool Contains(EventType eventType) => IsEmpty ? false : AsSpan().BinarySearch(eventType) >= 0;

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
