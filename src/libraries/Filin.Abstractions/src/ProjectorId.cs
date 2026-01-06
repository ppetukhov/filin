using System.Text;

namespace Filin;

/// <summary>
/// Идентификатор проектора.
/// </summary>
public readonly struct ProjectorId : IEquatable<ProjectorId>, IComparable<ProjectorId>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly Identity _value;

    /// <inheritdoc cref="ProjectorId"/>
    /// <param name="id">Идентификатор проектора.</param>
    public ProjectorId(string id) : this(new Identity(id)) { }

    /// <inheritdoc cref="ProjectorId"/>
    /// <param name="id">Идентификатор проектора.</param>
    /// <param name="version">Версия проектора.</param>
    public ProjectorId(string id, uint version) : this(version != 0 ? new Identity($"{id}@{version}") : new Identity(id)) { }

    ProjectorId(Identity rawIdentity) => _value = rawIdentity;

    /// <inheritdoc cref="Identity.IsDefault"/>
    public bool IsDefault => _value.IsDefault;

    /// <inheritdoc cref="Identity.AsSpan()"/>
    public ReadOnlySpan<byte> AsSpan() => _value.AsSpan();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProjectorId evetType && Equals(evetType);

    /// <inheritdoc/>
    public bool Equals(ProjectorId obj) => _value.Equals(obj._value);

    /// <inheritdoc cref="Identity.GetHashCode()"/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(ProjectorId other) => _value.CompareTo(other._value);

    /// <inheritdoc cref="Identity.ToString()"/>
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
    /// Конвертирует <see cref="ProjectorId"/> в <see cref="string"/>.
    /// </summary>
    /// <param name="projectorId"><inheritdoc cref="ProjectorId" path="/summary"/></param>
    /// <returns>Соответствующий <see cref="string"/>.</returns>
    public static implicit operator string?(ProjectorId projectorId) => projectorId.ToString();

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator ProjectorId(string id) => new(id);

    /// <inheritdoc cref="ToIdentity(ProjectorId)"/>
    public static explicit operator Identity(ProjectorId projectorId) => projectorId._value;

    /// <inheritdoc cref="FromIdentity(Identity)"/>
    public static explicit operator ProjectorId(Identity id) => new(id);

    /// <summary>
    /// Конвертирует <see cref="ProjectorId"/> в <see cref="ProjectorId"/>.
    /// </summary>
    /// <param name="id"><inheritdoc cref="ProjectorId(string)" path="/param[@name]"/></param>
    /// <returns>Соответствующий <see cref="ProjectorId"/>.</returns>
    public static ProjectorId FromString(string id) => new(id);

    /// <summary>
    /// Конвертирует <see cref="ProjectorId"/> в <see cref="Identity"/>.
    /// </summary>
    /// <param name="projectorId"><inheritdoc cref="ProjectorId(Identity)" path="/param[@rawIdentity]"/></param>
    /// <returns>Соответствующий <see cref="ProjectorId"/>.</returns>
    public static Identity ToIdentity(ProjectorId projectorId) => projectorId._value;

    /// <summary>
    /// Конвертирует <see cref="Identity"/> в <see cref="ProjectorId"/>.
    /// </summary>
    /// <param name="rawIdentity"><inheritdoc cref="ProjectorId(Identity)" path="/param[@rawIdentity]"/></param>
    /// <returns>Соответствующий <see cref="ProjectorId"/>.</returns>
    public static ProjectorId FromIdentity(Identity rawIdentity) => new(rawIdentity);

    /// <summary>
    /// Сравнивает предоставленные операнды на равенство.
    /// </summary>
    /// <param name="left">Левый операнд.</param>
    /// <param name="right">Правый операнд.</param>
    /// <returns><see langword="true"/> если предоставленные значения равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator ==(ProjectorId left, ProjectorId right) => left.Equals(right);

    /// <summary>
    /// Сравнивает предоставленные операнды на неравенство.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> если предоставленные значения не равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator !=(ProjectorId left, ProjectorId right) => !(left == right);

    public static bool operator <(ProjectorId left, ProjectorId right) => left.CompareTo(right) < 0;

    public static bool operator <=(ProjectorId left, ProjectorId right) => left.CompareTo(right) <= 0;

    public static bool operator >(ProjectorId left, ProjectorId right) => left.CompareTo(right) > 0;

    public static bool operator >=(ProjectorId left, ProjectorId right) => left.CompareTo(right) >= 0;
}
