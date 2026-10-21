using System.Runtime.CompilerServices;

namespace Filin.Storage;

/// <summary>
/// Represents the result of an event append operation submitted to the <see cref="IEventStore"/>.
/// </summary>
public readonly struct AppendResult : IEquatable<AppendResult>
{
    /// <summary>
    /// Represents a pre-allocated failed result indicating that a business conflict occurred when appending events.
    /// </summary>
    public static readonly AppendResult Conflict = new(true);

    /// <summary>
    /// Represents a pre-allocated successful result for an empty event append operation.
    /// </summary>
    public static readonly AppendResult Empty = new(false);

    readonly EventPosition _pos;
    readonly DateTimeOffset _ts;
    readonly object? _value; // Stores either a single DcbEvent or a IReadOnlyList<DcbEventSequenced> list.

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendResult"/> struct with a single appended event.
    /// </summary>
    /// <param name="event">The individual sequenced event instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if the underlying event payload is null.</exception>
    public AppendResult(DcbEventSequenced @event)
    {
        ArgumentNullException.ThrowIfNull(@event.Event);

        _value = @event.Event;
        _pos = @event.Position;
        _ts = @event.Timestamp;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendResult"/> struct with multiple appended events stored in a array.
    /// </summary>
    /// <param name="events">The sequential block containing additional appended events.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="events"/> is null.</exception>
    public AppendResult(IReadOnlyList<DcbEventSequenced> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            _value = Empty._value;
            _pos = Empty._pos;
            _ts = Empty._ts;
            return;
        }

        if (events.Count == 1)
        {
            var @event = events[0];
            _value = @event.Event;
            _pos = @event.Position;
            _ts = @event.Timestamp;
            return;
        }

        _value = events;
    }

    AppendResult(bool hasConflict) => _value = hasConflict ? null : (IReadOnlyList<DcbEventSequenced>)Array.Empty<DcbEventSequenced>().AsReadOnly();

    /// <summary>
    /// Gets the committed event at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the event to retrieve.</param>
    /// <returns>The <see cref="DcbEventSequenced"/> instance at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of bounds of the appended events collection.</exception>
    public DcbEventSequenced this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _value switch
            {
                DcbEvent evt => new(_pos, _ts, evt),
                IReadOnlyList<DcbEventSequenced> evts => evts[index],
                _ => throw InvalidStateException()
            };
        }
    }

    /// <summary>
    /// Gets the total number of successfully appended events.
    /// </summary>
    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value switch
        {
            DcbEvent => 1,
            IReadOnlyList<DcbEventSequenced> evts => evts.Count,
            _ => 0
        };
    }

    /// <summary>
    /// Gets a value indicating whether the event append operation completed successfully.
    /// </summary>
    public readonly bool IsSuccess => _value is DcbEvent or IReadOnlyList<DcbEventSequenced>;

    /// <summary>
    /// Gets a value indicating whether a conflict occurred.
    /// </summary>
    public readonly bool IsConflict => _value is null;

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AppendResultEnumerator GetEnumerator() =>
        _value switch
        {
            DcbEvent evt => new(_pos, _ts, evt),
            IReadOnlyList<DcbEventSequenced> evts => new(evts),
            _ => throw InvalidStateException()
        };

    /// <inheritdoc/>
    public bool Equals(AppendResult other) => _pos == other._pos && _ts == other._ts
        && (_value, other._value) switch
        {
            (DcbEvent evt, DcbEvent evt2) => evt.Equals(evt2),
            (IReadOnlyList<DcbEventSequenced> evts, IReadOnlyList<DcbEventSequenced> evts2) => evts.Count == evts2.Count && Enumerable.SequenceEqual(evts, evts2),
            (null, null) => true,
            _ => false
        };

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AppendResult res && Equals(res);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_pos, _ts, _value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static InvalidOperationException InvalidStateException() => new("The AppendResult state is invalid.");

    /// <summary>
    /// Determines whether two <see cref="AppendResult"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="AppendResult"/> instance to compare.</param>
    /// <param name="right">The second <see cref="AppendResult"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(AppendResult left, AppendResult right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="AppendResult"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="AppendResult"/> instance to compare.</param>
    /// <param name="right">The second <see cref="AppendResult"/> instance to compare.</param>
    /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(AppendResult left, AppendResult right) => !(left == right);
}
