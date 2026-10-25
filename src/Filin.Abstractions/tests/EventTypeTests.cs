using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class EventTypeTests
{
    const string Text1 = "Lorem Ipsum is simply dummy text of the printing and typesetting industry";
    const string Text2 = "Contrary to popular belief, Lorem Ipsum is not simply random text.";
    const string Text3 = "b0507063-77f0-48de-b005-88e35651ce30";
    const string Text4 = @"!""#$%&'()*+,-./:;<=>?@[\]^_`~";

    public static readonly TheoryData<string?, uint, bool> StringWithExpectedHashes = new()
    {
        { null, StableHash.Hash32((string?)null), true },
        { "", StableHash.Hash32(""), true },
        { Text1, StableHash.Hash32(Text1), false },
        { Text2, StableHash.Hash32(Text2), false },
        { Text3, StableHash.Hash32(Text3), false },
        { Text4, StableHash.Hash32(Text4), false },
    };

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = IdSpanTests.FormatWithFormatProviderAndExpectedString;

    [Theory]
    [MemberData(nameof(StringWithExpectedHashes))]
    public void EventType_Constructor_From_String(string? name, uint expectedHash, bool expectedIsDefault)
    {
        var expectedSpan = name.AsSpan();

        var result = new EventType(name!);

        result.IsDefault.Should().Be(expectedIsDefault);
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("test-value", "test-value")]
    public void EventType_Equals_Return_True(string? nameX, string? nameY)
    {
        var identityX = new EventType(nameX!);
        var identityY = new EventType(nameY!);

        var result = identityX.Equals(identityY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "any-data")]
    [InlineData(null, "")]
    [InlineData("any-data", null)]
    [InlineData("", null)]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void EventType_Equals_Return_False(string? nameX, string? nameY)
    {
        var identityX = new EventType(nameX!);
        var identityY = new EventType(nameY!);

        var result = identityX.Equals(identityY);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(-1, null, "")]
    [InlineData(1, "", null)]
    [InlineData(0, "", "")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-97, "abc", "abca")]
    [InlineData(97, "abca", "abc")]
    public void EventType_CompareTo(int expectedValue, string? nameX, string? nameY)
    {
        var identityX = new EventType(nameX!);
        var identityY = new EventType(nameY!);

        var result = identityX.CompareTo(identityY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void EventType_Value_Not_Null_When_IsDefault()
    {
        EventType identity = default;

        identity.Value.Should().NotBeNull(because: "EventType is default");
    }

    [Fact]
    public void EventType_Value_Return_Original_String()
    {
        var expectedString = "Some text";

        var identity = new EventType(expectedString);

        identity.Value.Should().BeSameAs(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var identity = new EventType("Some text");

        identity.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void EventType_ToString_With_Format_Return_EmptyString_When_EventType_IsDefault(string? format)
    {
        EventType identity = default;

        identity.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void EventType_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        EventType identity = default;

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var identity = new EventType("Some text for event type");

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_ISpanFormattable_TryFormat()
    {
        const string identityText = "Some text for event type";
        Span<char> destination = stackalloc char[64];

        var identity = new EventType(identityText);

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(identityText.Length);
        destination[..charsWritten].SequenceEqual(identityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var identity = new EventType("Some text for event type");

        var result = identity.TryFormat(destination, out var charsWritten, format, formatProvider);

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void EventType_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string identityText = "Some text for event type";
        Span<char> destination = stackalloc char[64];

        var identity = new EventType(identityText);

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(identityText.Length);
        destination[..charsWritten].SequenceEqual(identityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var identity = new EventType("Some text for event type");

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var identity = new EventType("Some text for event type");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void EventType_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        EventType identity = default;

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var identity = new EventType("Some text for event type");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_IUtf8SpanFormattable_TryFormat()
    {
        Span<byte> expected = stackalloc byte[64];
        Encoding.UTF8.TryGetBytes("Some text for event type".AsSpan(), expected, out var expBytes);

        Span<byte> utf8Destination = stackalloc byte[64];

        var eventType = new EventType("Some text for event type");

        var result = eventType.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(eventType.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected[..expBytes]).Should().BeTrue();
    }

    [Fact]
    public void EventType_FromString()
    {
        var expected = new EventType("Some text for event type");

        var result = EventType.FromString("Some text for event type");

        result.Should().BeEquivalentTo(expected);
    }
}
