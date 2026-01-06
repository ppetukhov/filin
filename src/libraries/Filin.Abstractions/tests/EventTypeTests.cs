using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class EventTypeTests
{
    public static readonly TheoryData<string, int> StringWithExpectedHashCode = IdentityTests.StringWithExpectedHashCode;

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = IdentityTests.FormatWithFormatProviderAndExpectedString;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EventType_Constructor_From_Null_Or_EmptyString(string? name)
    {
        var result = new EventType(name!);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.GetHashCode().Should().Be(StableHashTests.HASHCODE_OF_EMPTY);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void EventType_Constructor_From_String(string name, int expectedHashCode)
    {
        var expectedSpan = Encoding.UTF8.GetBytes(name).AsSpan();

        var result = new EventType(name);

        result.IsDefault.Should().BeFalse();
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.GetHashCode().Should().Be(expectedHashCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
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
    [InlineData(0, null, "")]
    [InlineData(0, "", "")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-1, "abc", "abca")]
    [InlineData(1, "abca", "abc")]
    public void EventType_CompareTo(int expectedValue, string? nameX, string? nameY)
    {
        var identityX = new EventType(nameX!);
        var identityY = new EventType(nameY!);

        var result = identityX.CompareTo(identityY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void EventType_ToString_Return_Null_When_IsDefault()
    {
        EventType identity = default;

        identity.ToString().Should().BeNull(because: "EventType is default");
    }

    [Fact]
    public void EventType_ToString_Return_Original_String()
    {
        var expectedString = "Some text";

        var identity = new EventType(expectedString);

        identity.ToString().Should().Be(expectedString);
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

        var identity = new EventType("Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_ISpanFormattable_TryFormat()
    {
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new EventType(IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var identity = new EventType("Some text identity");

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
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new EventType(IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void EventType_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var identity = new EventType("Some text identity");

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

        var identity = new EventType("Some text identity");

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

        var identity = new EventType("Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void EventType_IUtf8SpanFormattable_TryFormat()
    {
        Span<byte> utf8Destination = stackalloc byte[64];

        var identity = new EventType("Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(identity.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(identity.AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void EventType_FromString()
    {
        var expected = new EventType("Some text identity");

        var result = EventType.FromString("Some text identity");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void EventType_FromIdentity()
    {
        var expected = new EventType("Some text identity");

        var result = EventType.FromIdentity(new Identity("Some text identity"));

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void EventType_ToIdentity()
    {
        var expected = new Identity("Some text identity");

        var result = EventType.ToIdentity(new EventType("Some text identity"));

        result.Should().BeEquivalentTo(expected);
    }
}
