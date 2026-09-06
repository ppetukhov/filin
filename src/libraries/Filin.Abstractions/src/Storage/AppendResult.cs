using System.Runtime.CompilerServices;

namespace Filin.Storage;

/// <summary>
/// Represents the result of an event append operation submitted to the <see cref="IEventStore"/>.
/// </summary>
public readonly partial struct AppendResult
{
    /// <summary>
    /// Represents a pre-allocated failed result indicating that a business conflict occurred when appending events.
    /// </summary>
    public static readonly AppendResult Conflict;

    readonly object? _value; // Stores either a single DcbEvent or a DcbEventSequenced[] array.
    readonly EventPosition _pos;

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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendResult"/> struct with multiple appended events stored in a array.
    /// </summary>
    /// <param name="event">The first mandatory sequenced event.</param>
    /// <param name="events">The sequential block containing additional appended events.</param>
    /// <exception cref="ArgumentNullException">Thrown if the underlying event payload is null.</exception>
    public AppendResult(DcbEventSequenced @event, params ReadOnlySpan<DcbEventSequenced> events)
    {
        ArgumentNullException.ThrowIfNull(@event.Event);

        if (events.IsEmpty)
        {
            _value = @event.Event;
            _pos = @event.Position;
            return;
        }

        var arr = new DcbEventSequenced[1 + events.Length];

        arr[0] = @event;

        events.CopyTo(arr.AsSpan()[1..]);

        _value = arr;
    }

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
                DcbEvent evt => new(_pos, evt),
                DcbEventSequenced[] evts => evts[index],
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
        get
        {
            return _value switch
            {
                DcbEvent => 1,
                DcbEventSequenced[] evts => evts.Length,
                _ => 0
            };
        }
    }

    /// <summary>
    /// Gets a value indicating whether the event append operation completed successfully.
    /// </summary>
    public readonly bool IsSuccess => _value is not null;

    /// <summary>
    /// Gets a value indicating whether a conflict occurred.
    /// </summary>
    public readonly bool IsConflict => !IsSuccess;

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() =>
        _value switch
        {
            DcbEvent evt => new(evt, _pos),
            DcbEventSequenced[] evts => new(evts),
            _ => throw InvalidStateException()
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static InvalidOperationException InvalidStateException() => new("The AppendResult state is invalid.");
}
