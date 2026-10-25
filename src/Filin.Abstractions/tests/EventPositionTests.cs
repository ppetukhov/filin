using System.Globalization;
using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class EventPositionTests
{
    [Fact]
    public void EventPosition_Constructor_From_Zero()
    {
        var result = new EventPosition(0L);

        result.IsDefault.Should().BeTrue();
        result.Value.Should().Be(0L);
        result.GetHashCode().Should().Be(0L.GetHashCode());
    }

    [Fact]
    public void EventPosition_As_Default()
    {
        EventPosition result = default;

        result.IsDefault.Should().BeTrue();
        result.Value.Should().Be(0L);
        result.GetHashCode().Should().Be(0L.GetHashCode());
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

        result.IsDefault.Should().Be(isDefault);
        result.Value.Should().Be(value);
        result.GetHashCode().Should().Be(value.GetHashCode());
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

        result.Should().BeTrue();
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

        result.Should().BeFalse();
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

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void EventPosition_ToString_Return_Zero_When_IsDefault()
    {
        EventPosition position = default;

        position.ToString().Should().Be("0", because: "EventPosition is default");
    }

    [Fact]
    public void EventPosition_ToString_Return_Number_With_Format_D()
    {
        var expectedString = "1000000";

        var position = new EventPosition(1000000);

        position.ToString().Should().Be(expectedString);
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

        position.ToString(format: format, formatProvider: CultureInfo.InvariantCulture).Should().Be(expectedString);
    }

    [Fact]
    public void EventPosition_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[4];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void EventPosition_ISpanFormattable_TryFormat()
    {
        const string identityText = "1000000";
        Span<char> destination = stackalloc char[64];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(identityText.Length);
        destination[..charsWritten].SequenceEqual(identityText).Should().BeTrue();
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

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
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

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
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

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Fact]
    public void EventPosition_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[4];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void EventPosition_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("1000000");
        Span<byte> utf8Destination = stackalloc byte[64];

        var position = new EventPosition(1000000L);

        var result = position.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expected.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected.AsSpan()).Should().BeTrue();
    }
}
