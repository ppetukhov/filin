using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Filin.Storage;

/// <summary>
/// Represents the result of an event append operation submitted to the <see cref="IEventStore"/>.
/// </summary>
public readonly struct AppendResult
{
    /// <summary>
    /// Represents a pre-allocated faulted or failed append execution state.
    /// </summary>
    public static readonly AppendResult Faulted;

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
    public readonly bool IsFailure => !IsSuccess;

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

    /// <summary>
    /// Provides a stack-only enumerator for iterating through a sequence of <see cref="DcbEventSequenced"/> instances.
    /// </summary>
    public ref struct Enumerator
    {
        readonly DcbEventSequenced _single;
        readonly ReadOnlySpan<DcbEventSequenced> _values;
        int _index = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(DcbEvent value, EventPosition pos) => _single = new(pos, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<DcbEventSequenced> values) => _values = values;

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

            if (nextIndex < Count())
            {
                _index = nextIndex;
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="IEnumerator{DcbEventSequenced}.Current"/>
        public readonly DcbEventSequenced Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index < 0 || _index >= Count())
                {
                    ThrowInvalidOperationException();
                }

                return _single.Event is not null ? _single : _values[_index];
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        static void ThrowInvalidOperationException() => throw new InvalidOperationException("Enumerator.Current value has not been set.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly int Count() => _single.Event is not null ? 1 : _values.Length;
    }
}
