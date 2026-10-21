using System.Text;

namespace Filin;

/// <summary>
/// Represents a unique identifier for an event projector.
/// </summary>
public readonly struct ProjectorId : IEquatable<ProjectorId>, IComparable<ProjectorId>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly IdSpan _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorId"/> struct using a string identifier.
    /// </summary>
    /// <param name="id">The string representation of the projector identifier.</param>
    public ProjectorId(string id) : this(new IdSpan(id)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorId"/> struct using a string identifier and a specific version.
    /// </summary>
    /// <param name="id">The string representation of the projector identifier.</param>
    /// <param name="version">The version of the projector projection logic.</param>
    public ProjectorId(string id, uint version) : this(version != 0 ? new IdSpan($"{id}@{version}") : new IdSpan(id)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectorId"/> struct using a raw <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="rawIdSpan">The underlying raw identifier span.</param>
    ProjectorId(IdSpan rawIdSpan) => _value = rawIdSpan;

    /// <summary>
    /// Gets a value indicating whether the identifier is the default uninitialized instance.
    /// </summary>
    /// <value><see langword="true"/> if the identifier is uninitialized; otherwise, <see langword="false"/>.</value>
    public bool IsDefault => _value.IsDefault;

    /// <summary>
    /// Gets the string value of the projector identifier.
    /// </summary>
    public string Value => _value.Value;

    /// <summary>
    /// Gets the precomputed hash value of the identifier.
    /// </summary>
    public ulong Hash => _value.Hash;

    /// <summary>
    /// Returns a read-only character span representation of the underlying identifier value.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{Char}"/> over the identifier data.</returns>
    public ReadOnlySpan<char> AsSpan() => _value.AsSpan();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProjectorId evetType && Equals(evetType);

    /// <inheritdoc/>
    public bool Equals(ProjectorId obj) => _value.Equals(obj._value);

    /// <inheritdoc cref="IdSpan.GetHashCode()"/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(ProjectorId other) => _value.CompareTo(other._value);

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
    /// Implicitly converts a <see cref="ProjectorId"/> to its <see cref="string"/> representation.
    /// </summary>
    /// <param name="projectorId">The <see cref="ProjectorId"/> instance to convert.</param>
    public static implicit operator string?(ProjectorId projectorId) => projectorId.ToString();

    /// <inheritdoc cref="FromString(string)"/>
    public static implicit operator ProjectorId(string id) => new(id);

    /// <inheritdoc cref="ToIdSpan(ProjectorId)"/>
    public static explicit operator IdSpan(ProjectorId projectorId) => projectorId._value;

    /// <inheritdoc cref="FromIdSpan(IdSpan)"/>
    public static explicit operator ProjectorId(IdSpan id) => new(id);

    /// <summary>
    /// Factory method that constructs a <see cref="ProjectorId"/> from a string representation.
    /// </summary>
    /// <param name="id">The string representation of the projector identifier.</param>
    /// <returns>A new <see cref="ProjectorId"/> instance.</returns>
    public static ProjectorId FromString(string id) => new(id);

    /// <summary>
    /// Extracts the underlying <see cref="IdSpan"/> from a <see cref="ProjectorId"/>.
    /// </summary>
    /// <param name="projectorId">The projector identifier instance.</param>
    /// <returns>The underlying <see cref="IdSpan"/> wrapper.</returns>
    public static IdSpan ToIdSpan(ProjectorId projectorId) => projectorId._value;

    /// <summary>
    /// Constructs a <see cref="ProjectorId"/> from a raw <see cref="IdSpan"/>.
    /// </summary>
    /// <param name="rawIdSpan">The underlying raw identifier span.</param>
    /// <returns>A new <see cref="ProjectorId"/> instance wrapper.</returns>
    public static ProjectorId FromIdSpan(IdSpan rawIdSpan) => new(rawIdSpan);

    /// <summary>
    /// Compares two <see cref="ProjectorId"/> values for equality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ProjectorId left, ProjectorId right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="ProjectorId"/> values for inequality.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ProjectorId left, ProjectorId right) => !(left == right);

    /// <summary>
    /// Determines whether one <see cref="ProjectorId"/> instance is strictly less than another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is less than the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ProjectorId left, ProjectorId right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="ProjectorId"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is less than or equal to the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ProjectorId left, ProjectorId right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="ProjectorId"/> instance is strictly greater than another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is greater than the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ProjectorId left, ProjectorId right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="ProjectorId"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> if the left operand is greater than or equal to the right operand; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ProjectorId left, ProjectorId right) => left.CompareTo(right) >= 0;
}
