using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Filin;

/// <summary>
/// Represents a domain event type.
/// </summary>
/// <remarks>
/// <para>
/// This type implements <see cref="ISpanFormattable"/> and <see cref="IUtf8SpanFormattable"/> 
/// to support zero-allocation, high-performance string and UTF-8 formatting on the hot paths.
/// </para>
/// </remarks>
public readonly struct EventType : IEquatable<EventType>, IComparable<EventType>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly string? _value;

    readonly uint _hash;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventType"/> struct with a specified event type name.
    /// </summary>
    /// <param name="name">The name of the event type.</param>
    public EventType(string name) : this(name, StableHash.Hash32(name)) { }

    internal EventType(string name, uint hash) => (_value, _hash) = (name, hash);

    /// <inheritdoc cref="IdSpan.IsDefault"/>
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsDefault => string.IsNullOrEmpty(_value);

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public string? Value => _value;

    /// <summary>
    /// Returns a span representation of this instance.
    /// </summary>
    /// <returns>
    /// A span representation of this instance.
    /// </returns>
    public ReadOnlySpan<char> AsSpan() => _value.AsSpan();

    /// <summary>
    /// Hash value.
    /// </summary>
    public uint Hash => _hash;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EventType evetType && Equals(evetType);

    /// <inheritdoc/>
    public bool Equals(EventType obj) => string.Equals(_value, obj._value, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() => (int)_hash;

    /// <inheritdoc/>
    public int CompareTo(EventType other) => string.CompareOrdinal(_value, other._value);

    /// <inheritdoc/>
    public override string? ToString() => _value;

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format, this, formatProvider)
            : _value ?? string.Empty;

    /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<char> destination, out int charsWritten) => TryFormat(destination, out charsWritten, null, null);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter customFormatter)
        {
            var formattedString = customFormatter.Format(format.ToString(), this, provider);

            if (destination.Length < formattedString.Length)
            {
                charsWritten = 0;
                return false;
            }

            formattedString.AsSpan().CopyTo(destination);
            charsWritten = formattedString.Length;

            return true;
        }

        var span = AsSpan();

        if (span.TryCopyTo(destination))
        {
            charsWritten = span.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => TryFormat(utf8Destination, out bytesWritten, null, null);

    /// <inheritdoc/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var span = provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format.ToString(), this, provider).AsSpan()
            : AsSpan();

        return Encoding.UTF8.TryGetBytes(span, utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="EventType"/> struct without recalculating the hash.
    /// </summary>
    /// <param name="name">The name of the event type.</param>
    /// <param name="hash">The precomputed 32-bit hash retrieved during deserialization.</param>
    /// <returns>A new <see cref="EventType"/> instance initialized with the specified name and hash.</returns>
    /// <remarks>
    /// <para>
    /// This method is intended primarily for **infrastructure and deserialization** purposes. 
    /// It bypasses internal hash calculation to maximize performance when restoring the instance from an external store or payload.
    /// </para>
    /// <warning>
    /// Ensure that the provided <paramref name="hash"/> strictly correlates with the given <paramref name="name"/> 
    /// to prevent internal data consistency issues.
    /// </warning>
    /// </remarks>
    [Obsolete("This method is intended for infrastructure and deserialization only. Use the standard constructor or FromString for domain logic.")]
    public static EventType UnsafeCreate(string name, uint hash) => new EventType(name, hash);

    /// <summary>
    /// Defines an implicit conversion of an <see cref="EventType"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="eventType">The event type instance to convert.</param>
    /// <returns>The string representation of the event type, or <see langword="null"/> if uninitialized.</returns>
    public static implicit operator string?(EventType eventType) => eventType.Value;

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator EventType(string name) => new(name);

    /// <summary>
    /// Converts a <see cref="string"/> to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="name"><inheritdoc cref="EventType(string)" path="/param[@name]"/></param>
    /// <returns>The corresponding <see cref="EventType"/> instance.</returns>
    public static EventType FromString(string name) => new(name);

    /// <summary>
    /// Compares two <see cref="EventType"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(EventType left, EventType right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="EventType"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the specified instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(EventType left, EventType right) => !(left == right);

    /// <summary>
    /// Determines whether the first <see cref="EventType"/> instance is less than the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is less than the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(EventType left, EventType right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first <see cref="EventType"/> instance is less than or equal to the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is less than or equal to the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(EventType left, EventType right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the first <see cref="EventType"/> instance is greater than the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is greater than the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(EventType left, EventType right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first <see cref="EventType"/> instance is greater than or equal to the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is greater than or equal to the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(EventType left, EventType right) => left.CompareTo(right) >= 0;
}
