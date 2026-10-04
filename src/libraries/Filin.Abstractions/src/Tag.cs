using System.Text;

namespace Filin;

/// <summary>
/// Represents a unique metadata tag associated with domain events or queries.
/// </summary>
public readonly struct Tag : IEquatable<Tag>, IComparable<Tag>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly IdSpan _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct using a string name.
    /// </summary>
    /// <param name="name">The string representation of the tag name.</param>
    public Tag(string name) => _value = new(name);

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct using a raw <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="value">The underlying raw identifier span value.</param>
    Tag(IdSpan value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the tag is the default uninitialized instance.
    /// </summary>
    /// <value><see langword="true"/> if the tag is uninitialized; otherwise, <see langword="false"/>.</value>
    public bool IsDefault => _value.IsDefault;

    /// <summary>
    /// Gets the string representation of the tag value.
    /// </summary>
    public string Value => _value.Value;

    /// <summary>
    /// Gets the precomputed hash value of the tag.
    /// </summary>
    public ulong Hash => _value.Hash;

    /// <summary>
    /// Returns a read-only character span representation of the underlying tag value.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{Char}"/> over the tag data.</returns>
    public ReadOnlySpan<char> AsSpan() => _value.AsSpan();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Tag tag && Equals(tag);

    /// <inheritdoc/>
    public bool Equals(Tag obj) => _value.Equals(obj._value);

    /// <inheritdoc cref="IdSpan.GetHashCode()"/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(Tag other) => _value.CompareTo(other._value);

    /// <inheritdoc cref="IdSpan.ToString()"/>
    public override string? ToString() => _value.ToString();

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        (formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format, this, formatProvider)
            : ToString()) ?? string.Empty;

    /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<char> destination, out int charsWritten) => _value.TryFormat(destination, out charsWritten);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter)
        {
            var formattedString = formatter.Format(format.ToString(), this, provider);

            if (string.IsNullOrEmpty(formattedString))
            {
                charsWritten = 0;
                return true;
            }

            if (destination.Length < formattedString.Length)
            {
                charsWritten = 0;
                return false;
            }

            formattedString.CopyTo(destination);

            charsWritten = formattedString.Length;

            return true;
        }

        return TryFormat(destination, out charsWritten);
    }

    /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => _value.TryFormat(utf8Destination, out bytesWritten);

    /// <inheritdoc/>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter)
        {
            var formattedString = formatter.Format(format.ToString(), this, provider);

            if (string.IsNullOrEmpty(formattedString))
            {
                bytesWritten = 0;
                return true;
            }

            if (utf8Destination.Length < formattedString.Length)
            {
                bytesWritten = 0;
                return false;
            }

            return Encoding.UTF8.TryGetBytes(formattedString, utf8Destination, out bytesWritten);
        }

        return TryFormat(utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Tag"/> to its <see cref="string"/> representation.
    /// </summary>
    /// <param name="tag">The <see cref="Tag"/> instance to convert.</param>
    public static implicit operator string?(Tag tag) => tag.ToString();

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator Tag(string name) => new(name);

    /// <inheritdoc cref="ToIdSpan(Tag)"/>
    public static explicit operator IdSpan(Tag tag) => tag._value;

    /// <inheritdoc cref="FromIdSpan(IdSpan)"/>
    public static explicit operator Tag(IdSpan id) => new(id);

    /// <summary>
    /// Factory method that constructs a <see cref="Tag"/> from a string representation.
    /// </summary>
    /// <param name="name">The string representation of the tag name.</param>
    /// <returns>A new <see cref="Tag"/> instance.</returns>
    public static Tag FromString(string name) => new(name);

    /// <summary>
    /// Extracts the underlying <see cref="IdSpan"/> from a <see cref="Tag"/>.
    /// </summary>
    /// <param name="tag">The tag instance.</param>
    /// <returns>The underlying <see cref="IdSpan"/> wrapper.</returns>
    public static IdSpan ToIdSpan(Tag tag) => tag._value;

    /// <summary>
    /// Constructs a <see cref="Tag"/> from a raw <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="rawIdSpan">The underlying raw identifier span value.</param>
    /// <returns>A new <see cref="Tag"/> instance wrapper.</returns>
    public static Tag FromIdSpan(IdSpan rawIdSpan) => new(rawIdSpan);

    /// <summary>
    /// Compares the provided operands for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the provided values are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Tag left, Tag right) => left.Equals(right);

    /// <summary>
    /// Compares the provided operands for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the provided values are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Tag left, Tag right) => !(left == right);

    /// <summary>
    /// Determines whether one <see cref="Tag"/> instance is strictly less than another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is less than the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(Tag left, Tag right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="Tag"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is less than or equal to the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(Tag left, Tag right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="Tag"/> instance is strictly greater than another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is greater than the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(Tag left, Tag right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="Tag"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is greater than or equal to the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(Tag left, Tag right) => left.CompareTo(right) >= 0;
}
