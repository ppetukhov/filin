using System.Diagnostics;
using System.Runtime.CompilerServices;

using Filin.Internal;

namespace Filin;

/// <summary>
/// Represents a unique query item that encapsulates a combined specification of domain Tags 
/// and interested event types to form a specific dynamic consistency boundary section.
/// </summary>
/// <param name="EventTypes">The pre-sorted collection of domain event types.</param>
/// <param name="Tags">The pre-sorted collection of domain Tags.</param>
[DebuggerTypeProxy(typeof(DcbQueryItemDebugView))]
[DebuggerDisplay("{Internal.DcbQueryItemDebugView.GetDisplayString(this),nq}")]
public readonly record struct DcbQueryItem(EventTypeCollection EventTypes, TagCollection Tags) : IEquatable<DcbQueryItem>, IComparable<DcbQueryItem>
{
    /// <summary>
    /// Gets a value indicating whether the query item defines empty boundaries.
    /// </summary>
    public bool IsEmpty => Tags.IsEmpty && EventTypes.IsEmpty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DcbQueryItem"/> struct with specified event types and an empty tag boundary.
    /// </summary>
    /// <param name="eventTypes">The stack-allocated or heap-allocated span of interested event types.</param>
    public DcbQueryItem(params ReadOnlySpan<EventType> eventTypes)
        : this(new EventTypeCollection(eventTypes), default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DcbQueryItem"/> struct with specified domain Tags and an empty event type boundary.
    /// </summary>
    /// <param name="tags">The stack-allocated or heap-allocated span of query Tags.</param>
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

        EventTypeCollection mergedTypes = [.. this.EventTypes, .. eventTypes];
        return new DcbQueryItem(mergedTypes, Tags);
    }

    /// <summary>
    /// Combines the current query item boundaries with an additional set of domain Tags.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DcbQueryItem And(params ReadOnlySpan<Tag> tags)
    {
        if (tags.IsEmpty) return this;

        TagCollection mergedTags = [.. this.Tags, .. tags];
        return new DcbQueryItem(EventTypes, mergedTags);
    }

    /// <summary>
    /// Combines the current query item boundaries with another <see cref="DcbQueryItem"/> specification.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DcbQueryItem Add(DcbQueryItem other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;

        EventTypeCollection mergedTypes = [.. EventTypes, .. other.EventTypes];
        TagCollection mergedTags = [.. Tags, .. other.Tags];

        return new DcbQueryItem(mergedTypes, mergedTags);
    }

    /// <summary>
    /// Compares the current query item with another <see cref="DcbQueryItem"/> instance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DcbQueryItem other)
    {
        int typeComparison = EventTypes.CompareTo(other.EventTypes);
        if (typeComparison != 0)
        {
            return typeComparison;
        }

        return Tags.CompareTo(other.Tags);
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
        if (Tags.IsEmpty)
        {
            return true;
        }

        // 3. If the query requires specific Tags but the event has none
        if (@event.Tags.IsEmpty)
        {
            return false;
        }

        // 4. Verify that ALL query Tags (subset) are present within the event Tags (superset)
        var queryTagsSpan = Tags.AsSpan();
        var eventTagsSpan = @event.Tags.AsSpan();

        int i = 0; // Pointer for query Tags subset
        int j = 0; // Pointer for event Tags superset

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
    readonly bool Contains(EventType eventType) => EventTypes.Contains(eventType);

    /// <summary>
    /// Indicates whether the current query item boundary specification is equal to another.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(DcbQueryItem other) => EventTypes.Equals(other.EventTypes) && Tags.Equals(other.Tags);

    /// <summary>
    /// Returns the combined hash code for this boundary query item specification.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(EventTypes, Tags);
}
