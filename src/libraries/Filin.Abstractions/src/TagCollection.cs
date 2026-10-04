using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Internal;

namespace Filin;

/// <summary>
/// Represents a guaranteed pre-sorted, immutable collection of tags.
/// </summary>
[CollectionBuilder(typeof(TagCollection), nameof(Create))]
[DebuggerTypeProxy(typeof(TagCollectionDebugView))]
public readonly struct TagCollection : IEquatable<TagCollection>, IComparable<TagCollection>, ISpanFormattable
{
    readonly Tag[]? _tags;

    /// <summary>
    /// Gets a value indicating whether the collection is empty.
    /// </summary>
    /// <value><see langword="true"/> if the collection contains no tags; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(false, nameof(_tags))]
    public bool IsEmpty => _tags is not { Length: > 0 };

    /// <summary>
    /// Gets the total number of tags contained in the collection.
    /// </summary>
    /// <value>The count of unique tags.</value>
    public int Count => _tags?.Length ?? 0;

    /// <summary>
    /// Gets the <see cref="Tag"/> at the specified index within the sorted collection.
    /// </summary>
    /// <param name="index">The zero-based index of the tag to get.</param>
    /// <returns>The <see cref="Tag"/> element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or out of bounds.</exception>
    public Tag this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _tags![index];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TagCollection"/> struct by copying, 
    /// sorting, and deduplicating tags from a stack-allocated or heap-allocated span.
    /// </summary>
    /// <param name="tags">The span of tags to initialize the collection with.</param>
    public TagCollection(params ReadOnlySpan<Tag> tags)
    {
        if (tags.IsEmpty)
        {
            return;
        }

        if (tags.Length == 1)
        {
            _tags = [tags[0]];
            return;
        }

        _tags = ToSortedSet(tags.ToArray());
    }

    /// <summary>
    /// Factory method utilized automatically by the C# compiler to construct the type from collection expressions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagCollection Create(ReadOnlySpan<Tag> tags) => new(tags);

    /// <summary>
    /// Creates a new span over the collection.
    /// </summary>
    public ReadOnlySpan<Tag> AsSpan() => _tags.AsSpan();

    /// <summary>
    /// Determines whether the collection contains a specific tag.
    /// </summary>
    /// <param name="tag">The tag to locate in the collection.</param>
    /// <returns><see langword="true"/> if the tag is found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Tag tag) => IsEmpty ? false : AsSpan().BinarySearch(tag) >= 0;

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new(_tags);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(TagCollection other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(TagCollection other) => AsSpan().SequenceCompareTo(other.AsSpan());

    public override bool Equals(object? obj) => obj is TagCollection collection && Equals(collection);

    public override int GetHashCode() => IsEmpty ? 0 : CombineHashCode(_tags);

    /// <summary>
    /// Formats the collection of tags into the provided span of characters.
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
            var tagValueSpan = this[i].Value.AsSpan();

            if (destination[position..].Length < tagValueSpan.Length) return false;
            tagValueSpan.CopyTo(destination[position..]);
            position += tagValueSpan.Length;

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
    /// Formats the collection into a string layout using standard invariant format context rules.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format, this, formatProvider)
            : ToString();

    /// <summary>
    /// Returns a string that represents the current collection of tags, joined by the invariant culture list separator.
    /// Allocates strictly on the stack if the combined total length does not exceed 256 characters.
    /// </summary>
    /// <returns>A concatenated string of domain tags separated by the invariant list separator.</returns>
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

        // 1. Calculate the exact combined characters layout requirements
        var totalLength = 0;
        for (var i = 0; i < count; i++)
        {
            totalLength += this[i].Value.Length;
        }
        totalLength += (count - 1) * 2; // 2 chars per separator comma-and-space (", ")

        // 2. Stack allocation evaluation path (256 characters threshold)
        const int stackAllocThreshold = 256;
        if (totalLength <= stackAllocThreshold)
        {
            Span<char> buffer = stackalloc char[totalLength];
            TryFormat(buffer, out _, default, null);
            return new string(buffer);
        }

        // 3. Thread-safe rented array pool fallback for huge scale tag specifications
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

    static int CombineHashCode(ReadOnlySpan<Tag> _tags)
    {
        Debug.Assert(!_tags.IsEmpty);

        return _tags.Length switch
        {
            1 => _tags[0].GetHashCode(),
            2 => HashCode.Combine(_tags[0], _tags[1]),
            3 => HashCode.Combine(_tags[0], _tags[1], _tags[2]),
            4 => HashCode.Combine(_tags[0], _tags[1], _tags[2], _tags[3]),
            5 => HashCode.Combine(_tags[0], _tags[1], _tags[2], _tags[3], _tags[4]),
            _ => HashCode.Combine(_tags[0], _tags[1], _tags[2], _tags[3], _tags[4], CombineHashCode(_tags[5..])),
        };
    }

    static Tag[] ToSortedSet(Tag[] array)
    {
        array.Sort();

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
    /// Compares two <see cref="TagCollection"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(TagCollection left, TagCollection right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="TagCollection"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the collections are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(TagCollection left, TagCollection right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the first <see cref="TagCollection"/> instance precedes the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(TagCollection left, TagCollection right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first <see cref="TagCollection"/> instance precedes or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance precedes or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(TagCollection left, TagCollection right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the first <see cref="TagCollection"/> instance follows the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(TagCollection left, TagCollection right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first <see cref="TagCollection"/> instance follows or occurs in the same position as the second instance lexicographically.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance follows or equals the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(TagCollection left, TagCollection right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Provides a high-performance, stack-only enumerator over a collection.
    /// </summary>
    public ref struct Enumerator
    {
        int _index;
        readonly ReadOnlySpan<Tag> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<Tag> values)
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

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public readonly Tag Current
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
        static void ThrowInvalidOperationException() => throw new InvalidOperationException("TagCollection.Enumerator.Current value has not been set.");
    }
}
