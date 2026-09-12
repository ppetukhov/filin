using System.Diagnostics;
using System.Runtime.CompilerServices;

using Filin.Internal;

namespace Filin;

/// <summary>
/// Represents a unique query item that encapsulates a combined specification of domain tags 
/// and interested event types to form a specific dynamic consistency boundary section.
/// </summary>
/// <param name="eventTypes">The pre-sorted collection of domain event types.</param>
/// <param name="tags">The pre-sorted collection of domain tags.</param>
[DebuggerTypeProxy(typeof(DcbQueryItemDebugView))]
[DebuggerDisplay("{Internal.DcbQueryItemDebugView.GetDisplayString(this),nq}")]
public readonly record struct DcbQueryItem(EventTypeCollection eventTypes, TagCollection tags) : IEquatable<DcbQueryItem>, IComparable<DcbQueryItem>
{
    /// <summary>
    /// Gets the guaranteed sorted, immutable collection of query tags.
    /// </summary>
    public TagCollection Tags => tags;

    /// <summary>
    /// Gets the guaranteed sorted, immutable collection of interested event types.
    /// </summary>
    public EventTypeCollection EventTypes => eventTypes;

    /// <summary>
    /// Gets a value indicating whether the query item defines empty boundaries.
    /// </summary>
    public bool IsEmpty => tags.IsEmpty && eventTypes.IsEmpty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DcbQueryItem"/> struct with specified event types and an empty tag boundary.
    /// </summary>
    /// <param name="eventTypes">The stack-allocated or heap-allocated span of interested event types.</param>
    public DcbQueryItem(params ReadOnlySpan<EventType> eventTypes)
        : this(new EventTypeCollection(eventTypes), default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DcbQueryItem"/> struct with specified domain tags and an empty event type boundary.
    /// </summary>
    /// <param name="tags">The stack-allocated or heap-allocated span of query tags.</param>
    public DcbQueryItem(params ReadOnlySpan<Tag> tags)
        : this(default, new TagCollection(tags))
    {
    }

    /// <summary>
    /// Combines the current query item boundaries with an additional set of interested event types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DcbQueryItem Or(params ReadOnlySpan<EventType> eventTypes)
    {
        if (eventTypes.IsEmpty) return this;

        EventTypeCollection mergedTypes = [.. this.eventTypes, .. eventTypes];
        return new DcbQueryItem(mergedTypes, tags);
    }

    /// <summary>
    /// Combines the current query item boundaries with an additional set of domain tags.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DcbQueryItem And(params ReadOnlySpan<Tag> tags)
    {
        if (tags.IsEmpty) return this;

        TagCollection mergedTags = [.. this.tags, .. tags];
        return new DcbQueryItem(eventTypes, mergedTags);
    }

    /// <summary>
    /// Combines the current query item boundaries with another <see cref="DcbQueryItem"/> specification.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DcbQueryItem Add(DcbQueryItem other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;

        EventTypeCollection mergedTypes = [.. eventTypes, .. other.EventTypes];
        TagCollection mergedTags = [.. tags, .. other.Tags];

        return new DcbQueryItem(mergedTypes, mergedTags);
    }

    /// <summary>
    /// Compares the current query item with another <see cref="DcbQueryItem"/> instance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DcbQueryItem other)
    {
        // 1. Сначала лексикографически сравниваем коллекции типов событий
        int typeComparison = eventTypes.CompareTo(other.EventTypes);
        if (typeComparison != 0)
        {
            return typeComparison;
        }

        // 2. Если типы событий идентичны, сравниваем коллекции тегов
        return tags.CompareTo(other.Tags);
    }

    /// <summary>
    /// Determines whether the specified domain event matches the consistency constraints.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool MatchesEvent(DcbEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        // 1. Validate event type
        if (!IsEmpty && !Contains(@event.EventType))
        {
            return false;
        }

        // 2. If the query specifies no tag constraints, any event automatically qualifies
        if (tags.IsEmpty)
        {
            return true;
        }

        // 3. If the query requires specific tags but the event has none
        if (@event.Tags.IsEmpty)
        {
            return false;
        }

        // 4. Verify that ALL query tags (subset) are present within the event tags (superset)
        var queryTagsSpan = tags.AsSpan();
        var eventTagsSpan = @event.Tags.AsSpan();

        int i = 0; // Pointer for query tags subset
        int j = 0; // Pointer for event tags superset

        while (i < queryTagsSpan.Length && j < eventTagsSpan.Length)
        {
            int cmp = queryTagsSpan[i].CompareTo(eventTagsSpan[j]);

            if (cmp == 0)
            {
                i++;
                j++;
            }
            else if (cmp > 0)
            {
                j++;
            }
            else
            {
                return false;
            }
        }

        return i == queryTagsSpan.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly bool Contains(EventType eventType) => eventTypes.Contains(eventType);

    /// <summary>
    /// Indicates whether the current query item boundary specification is equal to another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(DcbQueryItem other) => eventTypes.Equals(other.EventTypes) && tags.Equals(other.Tags);

    /// <summary>
    /// Returns the combined hash code for this boundary query item specification.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(eventTypes, tags);
}
