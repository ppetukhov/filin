using System.Globalization;
using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class EventPositionTests
{
    [Fact]
    public void EventPosition_Constructor_From_Zero()
    {
        var result = new EventPosition(0L);

        Assert.True(result.IsDefault);
        Assert.Equal(0UL, result.Value);
        Assert.Equal(0L.GetHashCode(), result.GetHashCode());
    }

    [Fact]
    public void EventPosition_As_Default()
    {
        EventPosition result = default;

        Assert.True(result.IsDefault);
        Assert.Equal(0UL, result.Value);
        Assert.Equal(0L.GetHashCode(), result.GetHashCode());
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(10_000L)]
    [InlineData(ulong.MaxValue)]
    [InlineData(ulong.MinValue)]
    public void EventPosition_Constructor_From_LongValue(ulong value)
    {
        var result = new EventPosition(value);

        var isDefault = value == ulong.MinValue;

        Assert.Equal(isDefault, result.IsDefault);
        Assert.Equal(value, result.Value);
        Assert.Equal(value.GetHashCode(), result.GetHashCode());
    }

    [Theory]
    [InlineData(1L, 1L)]
    [InlineData(10_000L, 10_000L)]
    [InlineData(ulong.MaxValue, ulong.MaxValue)]
    [InlineData(ulong.MinValue, ulong.MinValue)]
    public void EventPosition_Equals_Return_True(ulong valueX, ulong valueY)
    {
        var positionX = new EventPosition(valueX);
        var positionY = new EventPosition(valueY);

        var result = positionX.Equals(positionY);

        Assert.True(result);
    }

    [Theory]
    [InlineData(1L, 0L)]
    [InlineData(10_000L, 10_001L)]
    [InlineData(ulong.MaxValue, ulong.MaxValue - 1)]
    [InlineData(ulong.MinValue, ulong.MinValue + 1)]
    public void Identity_Equals_Return_False(ulong valueX, ulong valueY)
    {
        var positionX = new EventPosition(valueX);
        var positionY = new EventPosition(valueY);

        var result = positionX.Equals(positionY);

        Assert.False(result);
    }

    [Theory]
    [InlineData(1, 10L, 1L)]
    [InlineData(-1, 10_000L, 10_100L)]
    [InlineData(1, ulong.MaxValue, ulong.MaxValue - 1)]
    [InlineData(-1, ulong.MinValue, ulong.MinValue + 1)]
    public void EventPosition_CompareTo(int expectedValue, ulong valueX, ulong valueY)
    {
        var positionX = new EventPosition(valueX);
        var positionY = new EventPosition(valueY);

        var result = positionX.CompareTo(positionY);

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void EventPosition_ToString_Return_Zero_When_IsDefault()
    {
        EventPosition position = default;

        Assert.Equal("0", position.ToString());
    }

    [Fact]
    public void EventPosition_ToString_Return_Number_With_Format_D()
    {
        var expectedString = "1000000";

        var position = new EventPosition(1000000);

        Assert.Equal(expectedString, position.ToString());
    }

    [Theory]
    [InlineData("G", 1000000L, "1000000")]
    [InlineData("D", 1000000L, "1000000")]
    [InlineData("F", 1000000L, "1000000.00")]
    [InlineData("N", 1000000L, "1,000,000.00")]
    [InlineData("N0", 1000000L, "1,000,000")]
    [InlineData("X", 1000000L, "F4240")]
    public void EventPosition_ToString_With_FormatProvider(string? format, ulong value, string expectedString)
    {
        var position = new EventPosition(value);

        Assert.Equal(expectedString, position.ToString(format: format, formatProvider: CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EventPosition_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[4];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(destination, out var charsWritten);

        Assert.False(result);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void EventPosition_ISpanFormattable_TryFormat()
    {
        const string identityText = "1000000";
        Span<char> destination = stackalloc char[64];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(destination, out var charsWritten);

        Assert.True(result);
        Assert.Equal(identityText.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(identityText));
    }

    [Theory]
    [InlineData("G", 1000000L, "1000000")]
    [InlineData("D", 1000000L, "1000000")]
    [InlineData("F", 1000000L, "1000000.00")]
    [InlineData("N", 1000000L, "1,000,000.00")]
    [InlineData("N0", 1000000L, "1,000,000")]
    [InlineData("X", 1000000L, "F4240")]
    public void EventPosition_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, ulong value, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var position = new EventPosition(value);

        var result = position.TryFormat(destination, out var charsWritten, format: format, provider: CultureInfo.InvariantCulture);

        Assert.True(result);
        Assert.Equal(expectedString.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(expectedString));
    }

    [Theory]
    [InlineData("G")]
    [InlineData("D")]
    [InlineData("F")]
    [InlineData("N")]
    [InlineData("N0")]
    [InlineData("X")]
    public void EventPosition_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format)
    {
        Span<char> destination = stackalloc char[4];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(destination, out var charsWritten, format: format, provider: CultureInfo.InvariantCulture);

        Assert.False(result);
        Assert.Equal(0, charsWritten);
    }

    [Theory]
    [InlineData("G", 1000000L, "1000000")]
    [InlineData("D", 1000000L, "1000000")]
    [InlineData("F", 1000000L, "1000000.00")]
    [InlineData("N", 1000000L, "1,000,000.00")]
    [InlineData("N0", 1000000L, "1,000,000")]
    [InlineData("X", 1000000L, "F4240")]
    public void EventPosition_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, ulong value, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var position = new EventPosition(value);

        var result = position.TryFormat(utf8Destination, out var bytesWritten, format: format, provider: CultureInfo.InvariantCulture);

        Assert.True(result);
        Assert.Equal(expectedBytes.Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(expectedBytes));
    }

    [Fact]
    public void EventPosition_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[4];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(utf8Destination, out var bytesWritten);

        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void EventPosition_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("1000000");
        Span<byte> utf8Destination = stackalloc byte[64];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(utf8Destination, out var bytesWritten);

        Assert.True(result);
        Assert.Equal(expected.AsSpan().Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(expected.AsSpan()));
    }
}
