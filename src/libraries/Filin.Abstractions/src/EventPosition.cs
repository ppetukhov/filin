using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// The absolute sequence index of the event within the event store log.
/// </summary>
/// <param name="Value">The raw absolute sequential index value.</param>
public readonly record struct EventPosition(ulong Value) : IEquatable<EventPosition>, IComparable<EventPosition>, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>
    /// Gets a value indicating whether the current <see cref="EventPosition"/> instance is equal to its default uninitialized state.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the current instance is equal to <see langword="default"/>; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDefault => default == Value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(EventPosition other) => Value.CompareTo(other.Value);

    /// <inheritdoc/>
    public override string ToString() => ToString("D", formatProvider: NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// Tries to format the value of the current <see cref="EventPosition"/> instance into the provided span of characters.
    /// </summary>
    /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination"/>.</param>
    /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten) => TryFormat(destination, out charsWritten, format: "D", provider: NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// Tries to format the value of the current <see cref="EventPosition"/> instance into the provided span of characters using the specified format and culture-specific formatting information.
    /// </summary>
    /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination"/>.</param>
    /// <param name="format">A read-only span of characters that specifies the format to use.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);

    /// <summary>
    /// Formats the value of the current <see cref="EventPosition"/> instance using the specified format and culture-specific formatting information.
    /// </summary>
    /// <param name="format">A string that specifies the format to use.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A string representation of the current instance's value as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.</returns>
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider) => Value.ToString(format, provider: formatProvider);

    /// <summary>
    /// Tries to format the value of the current <see cref="EventPosition"/> instance as UTF-8 into the provided span of bytes.
    /// </summary>
    /// <param name="utf8Destination">The span in which to write this instance's value formatted as a span of UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
    /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => TryFormat(utf8Destination, out bytesWritten, format: "D", provider: NumberFormatInfo.InvariantInfo);

    /// <summary>
    /// Tries to format the value of the current <see cref="EventPosition"/> instance as UTF-8 into the provided span of bytes using the specified format and culture-specific formatting information.
    /// </summary>
    /// <param name="utf8Destination">The span in which to write this instance's value formatted as a span of UTF-8 bytes.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
    /// <param name="format">A read-only span of characters that specifies the format to use.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

    /// <summary>
    /// Determines whether one <see cref="EventPosition"/> instance is less than another.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(EventPosition left, EventPosition right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one <see cref="EventPosition"/> instance is less than or equal to another.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(EventPosition left, EventPosition right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one <see cref="EventPosition"/> instance is greater than another.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(EventPosition left, EventPosition right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one <see cref="EventPosition"/> instance is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first instance to compare.</param>
    /// <param name="right">The second instance to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(EventPosition left, EventPosition right) => left.CompareTo(right) >= 0;
}
