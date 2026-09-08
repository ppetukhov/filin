using System.Text;

namespace Filin;

/// <summary>
/// Представляет тэг события.
/// </summary>
public readonly struct Tag : IEquatable<Tag>, IComparable<Tag>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly IdSpan _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct. 
    /// </summary>
    /// <param name="name">Название тэга.</param>
    public Tag(string name) => _value = new(name);

    /// <summary>
    /// Initializes a new instance of the <see cref="Tag"/> struct.
    /// </summary>
    /// <param name="value">Идентификатор тэга.</param>
    Tag(IdSpan value) => _value = value;

    /// <inheritdoc cref="IdSpan.IsDefault"/>
    public bool IsDefault => _value.IsDefault;

    public string? Value => _value.Value;

    public ulong Hash => _value.Hash;

    /// <inheritdoc cref="IdSpan.AsSpan()"/>
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
    /// Конвертирует <see cref="Tag"/> в <see cref="string"/>.
    /// </summary>
    /// <param name="tag"><inheritdoc cref="Tag" path="/summary"/></param>
    /// <returns>Соответствующий <see cref="string"/>.</returns>
    public static implicit operator string?(Tag tag) => tag.ToString();

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator Tag(string name) => new(name);

    /// <inheritdoc cref="ToIdSpan(Tag)"/>
    public static explicit operator IdSpan(Tag tag) => tag._value;

    /// <inheritdoc cref="FromIdSpan(IdSpan)"/>
    public static explicit operator Tag(IdSpan id) => new(id);

    /// <summary>
    /// Конвертирует <see cref="string"/> в <see cref="Tag"/>.
    /// </summary>
    /// <param name="name"><inheritdoc cref="Tag(string)" path="/param[@name]"/></param>
    /// <returns>Соответствующий <see cref="Tag"/>.</returns>
    public static Tag FromString(string name) => new(name);

    /// <summary>
    /// Конвертирует <see cref="Tag"/> в <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="tag"><inheritdoc cref="Tag(IdSpan)" path="/param[@rawIdSpan]"/></param>
    /// <returns>Соответствующий <see cref="Tag"/>.</returns>
    public static IdSpan ToIdSpan(Tag tag) => tag._value;

    /// <summary>
    /// Конвертирует <see cref="IdSpan"/> в <see cref="Tag"/>.
    /// </summary>
    /// <param name="rawIdSpan"><inheritdoc cref="Tag(IdSpan)" path="/param[@rawIdSpan]"/></param>
    /// <returns>Соответствующий <see cref="Tag"/>.</returns>
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

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator <(Tag left, Tag right) => left.CompareTo(right) < 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator <=(Tag left, Tag right) => left.CompareTo(right) <= 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator >(Tag left, Tag right) => left.CompareTo(right) > 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator >=(Tag left, Tag right) => left.CompareTo(right) >= 0;
}
