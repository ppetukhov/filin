using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Filin;

/// <summary>
/// Примитивный тип для идентификаторов, представляемыме последовательностью байт.
/// </summary>
public readonly struct Identity : IEquatable<Identity>, IComparable<Identity>, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>
    /// The stable hash of the underlying _value.
    /// </summary>
    readonly int _hashCode;

    /// <summary>
    /// The underlying _value.
    /// </summary>
    readonly byte[]? _value;

    /// <summary>
    /// Creates a new <see cref="Identity"/> instance from the provided _value.
    /// </summary>
    /// <returns>
    /// A new <see cref="Identity"/> corresponding to the provided id.
    /// </returns>
    public Identity(string? id) : this(value: ToValueBytes(id))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Identity"/> struct.
    /// </summary>
    /// <param name="value">
    /// The _value.
    /// </param>
    public Identity(byte[]? value) : this(value: value, hashCode: StableHash.ComputeHash(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Identity"/> struct.
    /// </summary>
    /// <param name="value">
    /// The _value.
    /// </param>
    /// <param name="hashCode">
    /// The hash code of the _value.
    /// </param>
    Identity(byte[]? value, int hashCode)
    {
        _value = value;
        _hashCode = hashCode;
    }

    /// <summary>
    /// Получает значение, указывающее, является ли данный экземпляр значением по умолчанию.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsDefault => _value is null || _value.Length == 0;

    /// <summary>
    /// Unsafe Initializes a new instance of the <see cref="Identity"/> struct.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="hashCode"></param>
    /// <returns></returns>
    public static Identity UnsafeCreate(byte[]? value, int hashCode) => new Identity(value, hashCode);

    /// <summary>
    /// Возвращает Span представляющий этот экземпляр.
    /// </summary>
    /// <returns>
    /// <see cref="ReadOnlySpan{Byte}"/> представляющий этот экземпляр.
    /// </returns>
    public ReadOnlySpan<byte> AsSpan() => _value;

    /// <summary>
    /// Стабильный хэшкод.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _hashCode;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Identity kind && Equals(kind);

    /// <inheritdoc/>
    public bool Equals(Identity obj) => _value == obj._value || _value.AsSpan().SequenceEqual(obj._value);

    /// <inheritdoc/>
    public int CompareTo(Identity other) => _value.AsSpan().SequenceCompareTo(other._value.AsSpan());

    /// <summary>
    /// Возвращает строку представляющую этот экземпляр.
    /// </summary>
    /// <returns>
    /// <see cref="string"/> представляющая этот экземпляр.
    /// </returns>
    public override string? ToString() => IsDefault ? null : Encoding.UTF8.GetString(_value);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        (formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format, this, formatProvider)
            : ToString()) ?? string.Empty;

    /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        if (IsDefault)
        {
            charsWritten = 0;
            return true;
        }

        if (destination.Length < _value.Length)
        {
            charsWritten = 0;
            return false;
        }

        return Encoding.UTF8.TryGetChars(_value, destination, out charsWritten);
    }

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
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten)
    {
        if (IsDefault)
        {
            bytesWritten = 0;
            return true;
        }

        if (utf8Destination.Length < _value.Length)
        {
            bytesWritten = 0;
            return false;
        }

        _value.CopyTo(utf8Destination);

        bytesWritten = _value.Length;

        return true;
    }

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

            return Encoding.UTF8.TryGetBytes(formattedString, utf8Destination, out bytesWritten);
        }

        return TryFormat(utf8Destination, out bytesWritten);
    }

    /// <summary>
    /// Конвертирует <see cref="string"/> в <see cref="Identity"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="Identity(string)" path="/param[@name]"/></param>
    /// <returns>Соответствующий <see cref="Identity"/>.</returns>
    public static Identity FromString(string value) => new(value);

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator Identity(string name) => new(name);

    /// <summary>
    /// Сравнивает предоставленные операнды на равенство.
    /// </summary>
    /// <param name="left">Левый операнд.</param>
    /// <param name="right">Правый операнд.</param>
    /// <returns><see langword="true"/> если предоставленные значения равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator ==(Identity left, Identity right) => left.Equals(right);

    /// <summary>
    /// Сравнивает предоставленные операнды на неравенство.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> если предоставленные значения не равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator !=(Identity left, Identity right) => !left.Equals(right);

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator <(Identity left, Identity right) => left.CompareTo(right) < 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator <=(Identity left, Identity right) => left.CompareTo(right) <= 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator >(Identity left, Identity right) => left.CompareTo(right) > 0;

    /// <inheritdoc cref="IComparable{T}" path="/summary"/>
    public static bool operator >=(Identity left, Identity right) => left.CompareTo(right) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[]? ToValueBytes(string? id) => string.IsNullOrEmpty(id) ? null : Encoding.UTF8.GetBytes(id);
}
