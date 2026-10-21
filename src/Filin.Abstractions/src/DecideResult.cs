using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Filin.Storage;

namespace Filin;

/// <summary>
/// Represents the result of a decider operation, containing either a request to append events to the <see cref="IEventStore"/> or a business error.
/// </summary>
public readonly struct DecideResult : IEquatable<DecideResult>
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
    /// <param name="errorCode"><inheritdoc cref="DomainRuleError.Code" path="/summary"/></param>
    /// <param name="errorMessage"><inheritdoc cref="DomainRuleError.Message" path="/summary"/></param>
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
    /// Gets the total number of successfully produced domain events.
    /// </summary>
    /// <value>The count of domain events if the operation succeeded; otherwise, 0.</value>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value switch
        {
            DcbEvent => 1,
            DcbEvent[] evts => evts.Length,
            _ => 0
        };
    }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully and yielded domain events.
    /// </summary>
    /// <value><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSuccess => _errorMessage is null && _value is not null;

    /// <summary>
    /// Gets a value indicating whether the operation failed due to a business error.
    /// </summary>
    /// <value><see langword="true"/> if the operation failed; otherwise, <see langword="false"/>.</value>
    [MemberNotNullWhen(true, nameof(_errorMessage), nameof(_value))]
    public bool IsFailure => _errorMessage is not null && _value is string;

    /// <summary>
    /// Gets the domain business error details associated with this failed operation result.
    /// </summary>
    /// <value>The structured <see cref="Filin.DomainRuleError"/> record containing failure context.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed when the operation completed successfully.</exception>
    public DomainRuleError Error
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_value, _errorMessage) switch
        {
            (string code, string msg) => new DomainRuleError(code, msg),
            (DcbEvent, null) => throw CannotRetrieveErrorException(),
            (DcbEvent[], null) => throw CannotRetrieveErrorException(),
            _ => throw UnreachableException()
        };
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DecideResultEnumerator GetEnumerator() =>
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
    /// <param name="code"><inheritdoc cref="DomainRuleError.Code" path="/summary"/></param>
    /// <param name="message"><inheritdoc cref="DomainRuleError.Message" path="/summary"/></param>
    /// <returns>A failed result instance containing the error.</returns>
    public static DecideResult Failure(string code, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentException.ThrowIfNullOrEmpty(message);

        return new(code, message);
    }

    /// <inheritdoc/>
    public bool Equals(DecideResult other) =>
        (_value, _errorMessage, other._value, other._errorMessage) switch
        {
            (string code, string msg, string code2, string msg2) => (code, msg) == (code2, msg2),
            (DcbEvent evt, null, DcbEvent evt2, null) => evt.Equals(evt2),
            (DcbEvent[] evts, null, DcbEvent[] evts2, null) => evts.AsSpan().SequenceEqual(evts2.AsSpan()),
            _ => false
        };

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DecideResult other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_value, _errorMessage);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a single <see cref="DcbEvent"/>.
    /// </summary>
    public static DecideResult FromDcbEvent(DcbEvent @event) => new(@event);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a domain rule error.
    /// </summary>
    public static DecideResult FromDomainRuleError(DomainRuleError error) => new(error.Code, error.Message);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from an error code and message tuple.
    /// </summary>
    public static DecideResult FromValueTuple((string Code, string Message) error) => new(error.Code, error.Message);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a tuple of two events.
    /// </summary>
    public static DecideResult FromValueTuple((DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a tuple of three events.
    /// </summary>
    public static DecideResult FromValueTuple((DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a tuple of four events.
    /// </summary>
    public static DecideResult FromValueTuple((DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3, events.Item4);

    /// <summary>
    /// Creates a <see cref="DecideResult"/> from a tuple of five events.
    /// </summary>
    public static DecideResult FromValueTuple((DcbEvent, DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => new(events.Item1, events.Item2, events.Item3, events.Item4, events.Item5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static InvalidOperationException CannotRetrieveErrorException() => new("Cannot retrieve an error from a successful result.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static UnreachableException UnreachableException() => new("The DecideResult state is invalid.");

    /// <summary>
    /// Determines whether two <see cref="DecideResult"/> instances are equal.
    /// </summary>
    public static bool operator ==(DecideResult left, DecideResult right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="DecideResult"/> instances are not equal.
    /// </summary>
    public static bool operator !=(DecideResult left, DecideResult right) => !(left == right);

    /// <summary>
    /// Implicitly converts a <see cref="DcbEvent"/> to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult(DcbEvent @event) => FromDcbEvent(@event);

    /// <summary>
    /// Implicitly converts a tuple of two events to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult((DcbEvent, DcbEvent) events) => FromValueTuple(events);

    /// <summary>
    /// Implicitly converts a tuple of three events to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent) events) => FromValueTuple(events);

    /// <summary>
    /// Implicitly converts a tuple of four events to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => FromValueTuple(events);

    /// <summary>
    /// Implicitly converts a tuple of five events to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult((DcbEvent, DcbEvent, DcbEvent, DcbEvent, DcbEvent) events) => FromValueTuple(events);

    /// <summary>
    /// Implicitly converts a domain rule error to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult(DomainRuleError error) => FromDomainRuleError(error);

    /// <summary>
    /// Implicitly converts an error tuple to a <see cref="DecideResult"/>.
    /// </summary>
    public static implicit operator DecideResult((string Code, string Message) error) => FromValueTuple(error);
}
