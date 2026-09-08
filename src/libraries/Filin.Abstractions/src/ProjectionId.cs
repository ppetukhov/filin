using System.Text;

namespace Filin;

/// <summary>
/// Идентификатор проекции.
/// </summary>
/// <remarks>
/// Creates a new <see cref="ProjectionId"/> instance.
/// </remarks>
public readonly struct ProjectionId(ProjectorId projectorId, IdSpan key) : IEquatable<ProjectionId>, IComparable<ProjectionId>, ISpanFormattable, IUtf8SpanFormattable
{
    readonly ProjectorId _projectorId = projectorId;
    readonly IdSpan _key = key;

    /// <summary>
    /// Gets <inheritdoc cref="Filin.ProjectorId" path="/summary"/>
    /// </summary>
    public ProjectorId ProjectorId => _projectorId;

    /// <summary>
    /// Gets the projection key.
    /// </summary>
    public IdSpan Key => _key;

    /// <inheritdoc cref="IdSpan.IsDefault"/>
    public bool IsDefault => _projectorId.IsDefault && _key.IsDefault;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProjectionId id && Equals(id);

    /// <inheritdoc/>
    public bool Equals(ProjectionId obj) => _key.Equals(obj._key) && _projectorId.Equals(obj._projectorId);

    /// <inheritdoc cref="IdSpan.GetHashCode()"/>
    public override int GetHashCode() => _projectorId.GetHashCode() * 31 + _key.GetHashCode(); // stable hashcode.

    /// <inheritdoc/>
    public int CompareTo(ProjectionId other)
    {
        var cmp = _projectorId.CompareTo(other._projectorId);

        return cmp is 0 ? _key.CompareTo(other._key) : cmp;
    }

    /// <summary>
    /// Deconstruct to Tuple.
    /// </summary>
    /// <param name="projectorId"><inheritdoc cref="Filin.ProjectorId" path="/summary"/></param>
    /// <param name="key"><inheritdoc cref="IdSpan" path="/summary"/></param>
    public void Deconstruct(out ProjectorId projectorId, out IdSpan key)
    {
        projectorId = _projectorId;
        key = _key;
    }

    /// <inheritdoc cref="IdSpan.ToString()"/>
    public override string? ToString() => _key.IsDefault ? _projectorId.ToString() : $"{_projectorId}/{_key}";

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        (formatProvider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter
            ? formatter.Format(format, this, formatProvider)
            : ToString()) ?? string.Empty;

    /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>
    public bool TryFormat(Span<char> destination, out int charsWritten)
    {
        if (destination.Length < _projectorId.AsSpan().Length + (_key.IsDefault ? 0 : 1 + _key.AsSpan().Length))
        {
            charsWritten = 0;
            return false;
        }

        if (!_projectorId.TryFormat(destination, out var count))
        {
            charsWritten = count;
            return false;
        }

        charsWritten = count;

        if (_key.IsDefault)
        {
            return true;
        }

        var delim = '/';

        destination = destination[count..];

        if (!new Span<char>(ref delim).TryCopyTo(destination))
        {
            return false;
        }

        charsWritten += 1;

        destination = destination[1..];

        if (!_key.TryFormat(destination, out count))
        {
            charsWritten += count;
            return false;
        }

        charsWritten += count;

        return true;
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
        var delim = (byte)'/';

        if (!_projectorId.TryFormat(utf8Destination, out bytesWritten))
        {
            return false;
        }

        if (_key.IsDefault)
        {
            return true;
        }

        utf8Destination = utf8Destination[bytesWritten..];

        if (!new Span<byte>(ref delim).TryCopyTo(utf8Destination))
        {
            return false;
        }

        bytesWritten += 1;

        utf8Destination = utf8Destination[1..];

        if (!_key.TryFormat(utf8Destination, out var cnt))
        {
            return false;
        }

        bytesWritten += cnt;

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
    /// Конвертирует <see cref="ProjectionId"/> в <see cref="string"/>.
    /// </summary>
    /// <param name="value"><inheritdoc cref="ProjectionId" path="/summary"/></param>
    /// <returns>Соответствующий <see cref="string"/>.</returns>
    public static implicit operator string?(ProjectionId value) => value.ToString();

    /// <inheritdoc cref="FromTuple(ValueTuple{ProjectorId, IdSpan})"/>
    public static explicit operator ProjectionId((ProjectorId Projector, IdSpan Key) tuple) => new(tuple.Projector, tuple.Key);

    /// <summary>
    /// Конвертирует <see cref="ValueTuple{ProjectorId, IdSpan}"/> в <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="tuple">Projector tuple</param>
    /// <returns>Соответствующий <see cref="ProjectorId"/>.</returns>
    public static ProjectionId FromTuple((ProjectorId Projector, IdSpan Key) tuple) => new(tuple.Projector, tuple.Key);

    /// <summary>
    /// Сравнивает предоставленные операнды на равенство.
    /// </summary>
    /// <param name="left">Левый операнд.</param>
    /// <param name="right">Правый операнд.</param>
    /// <returns><see langword="true"/> если предоставленные значения равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator ==(ProjectionId left, ProjectionId right) => left.Equals(right);

    /// <summary>
    /// Сравнивает предоставленные операнды на неравенство.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> если предоставленные значения не равны, в противном случае <see langword="false"/>.</returns>
    public static bool operator !=(ProjectionId left, ProjectionId right) => !(left == right);

    public static bool operator <(ProjectionId left, ProjectionId right) => left.CompareTo(right) < 0;

    public static bool operator <=(ProjectionId left, ProjectionId right) => left.CompareTo(right) <= 0;

    public static bool operator >(ProjectionId left, ProjectionId right) => left.CompareTo(right) > 0;

    public static bool operator >=(ProjectionId left, ProjectionId right) => left.CompareTo(right) >= 0;
}
