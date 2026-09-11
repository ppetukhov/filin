using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Represents a guaranteed pre-sorted, immutable collection of tags.
/// </summary>
[CollectionBuilder(typeof(TagCollection), nameof(Create))]
public readonly struct TagCollection : IEquatable<TagCollection>, IComparable<TagCollection>
{
    readonly Tag[]? _tags;

    [MemberNotNullWhen(false, nameof(_tags))]
    public bool IsEmpty => _tags is not { Length: > 0 };

    public int Count => _tags?.Length ?? 0;

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
    /// <returns></returns>
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

    static bool IsMatchTagParam(global::Filin.IdSpan tagParam, global::Filin.Tag tag) => tag.AsSpan().StartsWith(tagParam.AsSpan());

    internal static global::Filin.DcbQuery CreateQueryCore(global::System.Collections.Immutable.ImmutableArray<global::Filin.DcbQueryItem> queryParts, global::System.Collections.Immutable.ImmutableArray<(global::Filin.IdSpan, global::System.Collections.Immutable.ImmutableArray<int>)> queryTagParams, global::System.Collections.Immutable.ImmutableSortedSet<global::Filin.Tag> tags)
    {
        var queryItems = queryParts.ToList();
        foreach (var tagParam in queryTagParams)
        {
            var tagFound = false;
            foreach (var tag in tags)
            {
                if (IsMatchTagParam(tagParam.Item1, tag))
                {
                    foreach (var queryIdx in tagParam.Item2)
                    {
                        var (qEventTypes, qTags) = queryItems[queryIdx];

                        queryItems[queryIdx] = new(qEventTypes, [.. qTags, tag]);
                    }

                    tagFound = true;
                }
            }

            if (!tagFound)
            {
                var ex = new global::System.ArgumentException($"Required tag started with {tagParam.Item1}", nameof(tags));
                ex.Data["TagParameter"] = tagParam.Item1.ToString();
                ex.Data["Tags"] = string.Join(";", tags);
                throw ex;
            }
        }

        return new global::Filin.DcbQuery([.. queryItems]);
    }
}
