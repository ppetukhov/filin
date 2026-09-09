using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Filin.Storage;

namespace Filin;

/// <summary>
/// Фильтр событий в <see cref="IEventStore"/>.
/// </summary>
/// <remarks>Элементы фильтра <see cref="DcbQueryItem"/> применяются с оператором <see langword="or"/>.</remarks>
/// <param name="Items">Коллеция элементов фильтра.</param>
public sealed partial record DcbQuery(ImmutableHashSet<DcbQueryItem> Items)
{
    /// <summary>
    /// Любое событие.
    /// </summary>
    public static readonly DcbQuery All = new([]);

    /// <summary>
    /// Является запросом на выборку любого события.
    /// </summary>
    public bool IsAll => Items.IsEmpty || Items.Contains(DcbQueryItem.Any);

    public static DcbQuery FromItems(IEnumerable<DcbQueryItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items is ImmutableHashSet<DcbQueryItem> inset)
        {
            return inset.IsEmpty || inset.Contains(DcbQueryItem.Any) ? All : new DcbQuery(inset);
        }

        ImmutableHashSet<DcbQueryItem> set = [];

        var builder = set.ToBuilder();

        foreach (var item in items)
        {
            if (item.IsAny) return All;

            builder.Add(item);
        }

        return builder.Count == 0 ? All : new DcbQuery(builder.ToImmutable());
    }

    public bool MatchesEvent(DcbEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (IsAll) return true;

        foreach (var queryItem in Items)
        {
            if (queryItem.MatchesEvent(@event)) return true;
        }

        return false;
    }

    public DcbQuery Or(DcbQueryItem queryItem)
    {
        ArgumentNullException.ThrowIfNull(queryItem);

        return queryItem.IsAny || IsAll ? All : this with { Items = Items.Add(queryItem) };
    }

    public DcbQuery Or(DcbQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.IsAll || IsAll ? All : this with { Items = Items.Union(query.Items) };
    }

    public DcbQuery Compact()
    {
        if (IsAll)
        {
            return All;
        }

        var builder = ImmutableHashSet.CreateBuilder<DcbQueryItem>();

        foreach (var grouped in Items.GroupBy(static x => x.Tags, TagSetEqualityComparer.Instance))
        {
            var eventTypes = grouped.SelectMany(static q => q.EventTypes).ToImmutableHashSet();

            builder.Add(new(EventTypes: eventTypes, Tags: grouped.Key));
        }

        return new DcbQuery(builder.ToImmutable());
    }

    public static DcbQuery FromImmutableHashSet(ImmutableHashSet<DcbEvent> dcbEvents)
    {
        ArgumentNullException.ThrowIfNull(dcbEvents);

        var queryItems = ImmutableHashSet<DcbQueryItem>.Empty.ToBuilder();

        foreach (var dcbEvent in dcbEvents)
        {
            var queryItem = dcbEvent.ToDcbQueryItem();

            if (queryItems.FirstOrDefault(x => x.Tags.SetEquals(queryItem.Tags)) is { } existWithSameTags)
            {
                _ = queryItems.Remove(existWithSameTags);

                queryItem = existWithSameTags.Or(queryItem.EventTypes);
            }

            queryItems.Add(queryItem);
        }

        return new DcbQuery(queryItems.ToImmutable());
    }

    public static implicit operator DcbQuery(ImmutableHashSet<DcbEvent> dcbEvents) => FromImmutableHashSet(dcbEvents);

    /// <inheritdoc/>
    sealed class TagSetEqualityComparer : IEqualityComparer<ImmutableSortedSet<Tag>>
    {
        internal static readonly TagSetEqualityComparer Instance = new TagSetEqualityComparer();

        /// <inheritdoc/>
        public bool Equals(ImmutableSortedSet<Tag>? x, ImmutableSortedSet<Tag>? y) =>
            x is not null && y is not null && x.Count == y.Count && x.All(y.Contains)
            || x is null && y is null;

        /// <inheritdoc/>
        public int GetHashCode([DisallowNull] ImmutableSortedSet<Tag> obj)
        {
            var hashCode = 0;

            foreach (var tag in obj)
            {
                hashCode = HashCode.Combine(hashCode, tag);
            }

            return hashCode;
        }
    }
}
