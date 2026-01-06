using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Storage;

namespace Filin;

/// <summary>
/// Represents the result of a decider operation, containing either a request to append events to the <see cref="IEventStore"/> or a business error.
/// </summary>
public readonly struct DecideResult
{
    readonly object? _value; // Can store a DcbEvent, a DcbEvent[], or an error Code string.
    readonly string? _errorMessage; // Stores the Message if it is an error

    /// <summary>
    /// Initializes a new instance of the class representing a successful request to append event.
    /// </summary>
    /// <param name="event">First event.</param>
    public DecideResult(DcbEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _value = @event;
    }

    /// <summary>
    /// Initializes a new instance of the class representing a successful request to append events.
    /// </summary>
    /// <param name="event">First event.</param>
    /// <param name="events">Next events.</param>
    public DecideResult(DcbEvent @event, params ReadOnlySpan<DcbEvent> events)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (events.IsEmpty)
        {
            _value = @event;
            return;
        }

        var arr = new DcbEvent[1 + events.Length];

        arr[0] = @event;

        events.CopyTo(arr.AsSpan()[1..]);

        _value = arr;
    }

    /// <summary>
    /// Initializes a new instance of the class representing a failed operation with a business error.
    /// </summary>
    /// <param name="errorCode"><inheritdoc cref="Error.Code" path="/summary"/></param>
    /// <param name="errorMessage"><inheritdoc cref="Error.Message" path="/summary"/></param>
    public DecideResult(string errorCode, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        _value = errorCode;
        _errorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the appended event at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the event to get.</param>
    /// <returns>The <see cref="DcbEventSequenced"/> instance at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of bounds.</exception>
    public DcbEvent this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            return _value switch
            {
                DcbEvent evt => evt,
                DcbEvent[] evts => evts[index],
                _ => throw UnreachableException()
            };
        }
    }

    /// <summary>
    /// Gets the total number of events.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _value switch
            {
                DcbEvent => 1,
                DcbEvent[] evts => evts.Length,
                _ => 0
            };
        }
    }


    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess => _errorMessage is null && _value is not null;

    [MemberNotNullWhen(true, nameof(_errorMessage), nameof(_value))]
    public bool IsFailure => _errorMessage is not null && _value is string;

    /// <summary>
    /// Gets the domain business error details associated with this failed operation result.
    /// </summary>
    /// <value>The structured <see cref="Filin.Error"/> record containing failure context.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed when the operation completed successfully.</exception>
    public Error Error
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return (_value, _errorMessage) switch
            {
                (string code, string msg) => new Error(code, msg),
                (DcbEvent, null) => throw CannotRetrieveErrorException(),
                (DcbEvent[], null) => throw CannotRetrieveErrorException(),
                _ => throw UnreachableException()
            };
        }
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator<DcbEvent> GetEnumerator() =>
        _value switch
        {
            DcbEvent evt => new(evt),
            DcbEvent[] evts => new(evts),
            _ => new([])
        };

    /// <summary>
    /// Creates a successful result containing the request to append events.
    /// </summary>
    /// <param name="event">First event.</param>
    /// <param name="events">Next events.</param>
    /// <returns>A successful result instance.</returns>
    public static DecideResult Success(DcbEvent @event, params ReadOnlySpan<DcbEvent> events) => new(@event, events);

    /// <summary>
    /// Creates a failed result containing the domain business error.
    /// </summary>
    /// <param name="code"><inheritdoc cref="Error.Code" path="/summary"/></param>
    /// <param name="message"><inheritdoc cref="Error.Message" path="/summary"/></param>
    /// <returns>A failed result instance containing the error.</returns>
    public static DecideResult Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentException.ThrowIfNullOrEmpty(message);

        return new(code, message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static InvalidOperationException CannotRetrieveErrorException() => new InvalidOperationException("Cannot retrieve an error from a successful result.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static UnreachableException UnreachableException() => new UnreachableException("The DecideResult state is invalid.");

    public static implicit operator DecideResult(DcbEvent @event) => new(@event);

    public static implicit operator DecideResult((DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2);

    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3);

    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3, events.Item4);

    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3, events.Item4, events.Item5);

    public static implicit operator DecideResult(Error error) => new(error.Code, error.Message);

    public static implicit operator DecideResult((string Code, string Message) error) => new(error.Code, error.Message);
}
