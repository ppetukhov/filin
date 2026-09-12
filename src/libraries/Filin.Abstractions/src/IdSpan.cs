using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Filin;

/// <summary>
/// Primitive type for identities, representing a sequence of chars.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="IdSpan"/> struct.
/// </para>
/// <para>
/// This type implements <see cref="ISpanFormattable"/> and <see cref="IUtf8SpanFormattable"/> 
/// to support zero-allocation, high-performance string and UTF-8 formatting on the hot paths.
/// </para>
/// </remarks>
public readonly struct IdSpan : IEquatable<IdSpan>, IComparable<IdSpan>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly string? _value;

    readonly ulong _hash;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdSpan"/> struct with a specified id.
    /// </summary>
    /// <param name="id">The id value.</param>
    public IdSpan(string id) : this(id, StableHash.Hash64(id)) { }

    internal IdSpan(string id, ulong hash) => (_value, _hash) = (id, hash);

    /// <summary>
    /// Gets a value indicating whether this instance is the default value.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsDefault => string.IsNullOrEmpty(_value);

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public string Value => _value ?? string.Empty;

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
    public ulong Hash => _hash;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is IdSpan evetType && Equals(evetType);

    /// <inheritdoc/>
    public bool Equals(IdSpan obj) => string.Equals(_value, obj._value, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() => _hash.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(IdSpan other) => string.CompareOrdinal(_value, other._value);

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
    /// Creates a new instance of the <see cref="IdSpan"/> struct without recalculating the hash.
    /// </summary>
    /// <param name="id">The name of the event type.</param>
    /// <param name="hash">The precomputed 32-bit hash retrieved during deserialization.</param>
    /// <returns>A new <see cref="IdSpan"/> instance initialized with the specified name and hash.</returns>
    /// <remarks>
    /// <para>
    /// This method is intended primarily for **infrastructure and deserialization** purposes. 
    /// It bypasses internal hash calculation to maximize performance when restoring the instance from an external store or payload.
    /// </para>
    /// <warning>
    /// Ensure that the provided <paramref name="hash"/> strictly correlates with the given <paramref name="id"/> 
    /// to prevent internal data consistency issues.
    /// </warning>
    /// </remarks>
    [Obsolete("This method is intended for infrastructure and deserialization only. Use the standard constructor or FromString for domain logic.")]
    public static IdSpan UnsafeCreate(string id, ulong hash) => new IdSpan(id, hash);

    /// <summary>
    /// Defines an implicit conversion of an <see cref="IdSpan"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="IdSpan">The event type instance to convert.</param>
    /// <returns>The string representation of the event type, or <see langword="null"/> if uninitialized.</returns>
    public static implicit operator string(IdSpan IdSpan) => IdSpan.Value;

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator IdSpan(string id) => new(id);

    /// <summary>
    /// Converts a <see cref="string"/> to an <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="id"><inheritdoc cref="IdSpan(string)" path="/param[@name]"/></param>
    /// <returns>The corresponding <see cref="IdSpan"/> instance.</returns>
    public static IdSpan FromString(string id) => new(id);

    /// <summary>
    /// Compares two <see cref="IdSpan"/> instances for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the specified instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(IdSpan left, IdSpan right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="IdSpan"/> instances for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the specified instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(IdSpan left, IdSpan right) => !(left == right);

    /// <summary>
    /// Determines whether the first <see cref="IdSpan"/> instance is less than the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is less than the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(IdSpan left, IdSpan right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first <see cref="IdSpan"/> instance is less than or equal to the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is less than or equal to the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(IdSpan left, IdSpan right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the first <see cref="IdSpan"/> instance is greater than the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is greater than the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(IdSpan left, IdSpan right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first <see cref="IdSpan"/> instance is greater than or equal to the second instance.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left instance is greater than or equal to the right instance; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(IdSpan left, IdSpan right) => left.CompareTo(right) >= 0;
}
