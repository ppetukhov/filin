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
    public bool IsDefault => default == Value;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(EventPosition other) => Value.CompareTo(other.Value);

    public override string ToString() => ToString("D", formatProvider: NumberFormatInfo.InvariantInfo);

    public bool TryFormat(Span<char> destination, out int charsWritten) => TryFormat(destination, out charsWritten, format: "D", provider: NumberFormatInfo.InvariantInfo);

    public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider) => Value.ToString(format, provider: formatProvider);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => TryFormat(utf8Destination, out bytesWritten, format: "D", provider: NumberFormatInfo.InvariantInfo);

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(utf8Destination, out bytesWritten, format, provider);

    public static bool operator <(EventPosition left, EventPosition right) => left.CompareTo(right) < 0;

    public static bool operator <=(EventPosition left, EventPosition right) => left.CompareTo(right) <= 0;

    public static bool operator >(EventPosition left, EventPosition right) => left.CompareTo(right) > 0;

    public static bool operator >=(EventPosition left, EventPosition right) => left.CompareTo(right) >= 0;
}
