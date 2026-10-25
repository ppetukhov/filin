using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class TagTests
{
    public static readonly TheoryData<string, ulong> StringWithExpectedHashCode = IdSpanTests.StringWithExpectedHashCode;

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = IdSpanTests.FormatWithFormatProviderAndExpectedString;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Tag_Constructor_From_Null_Or_EmptyString(string? name)
    {
        var result = new Tag(name!);

        result.IsDefault.Should().BeTrue();
        result.Value.Should().BeSameAs(string.Empty);
        result.AsSpan().Length.Should().Be(0);
        result.Hash.Should().Be(StableHashTests.HASH64_OF_EMPTY);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void Tag_Constructor_From_String(string name, ulong expectedHash)
    {
        var result = new Tag(name);

        result.IsDefault.Should().BeFalse();
        result.Value.Should().BeSameAs(name);
        result.AsSpan().SequenceEqual(name.AsSpan()).Should().BeTrue();
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("test-value", "test-value")]
    public void Tag_Equals_Return_True(string? nameX, string? nameY)
    {
        var idSpanX = new Tag(nameX!);
        var idSpanY = new Tag(nameY!);

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "any-data")]
    [InlineData(null, "")]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void Tag_Equals_Return_False(string? nameX, string? nameY)
    {
        var idSpanX = new Tag(nameX!);
        var idSpanY = new Tag(nameY!);

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(-1, null, "")]
    [InlineData(0, "", "")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-97, "abc", "abca")]
    [InlineData(97, "abca", "abc")]
    public void Tag_CompareTo(int expectedValue, string? nameX, string? nameY)
    {
        var idSpanX = new Tag(nameX!);
        var idSpanY = new Tag(nameY!);

        var result = idSpanX.CompareTo(idSpanY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void Tag_ToString_Return_Null_When_IsDefault()
    {
        Tag idSpan = default;

        idSpan.ToString().Should().BeNull(because: "Tag is default");
    }

    [Fact]
    public void Tag_ToString_Return_Original_String()
    {
        var expectedString = "Some text";

        var idSpan = new Tag(expectedString);

        idSpan.ToString().Should().Be(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var idSpan = new Tag("Some text");

        idSpan.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void Tag_ToString_With_Format_Return_EmptyString_When_EventType_IsDefault(string? format)
    {
        Tag idSpan = default;

        idSpan.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Tag_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        Tag idSpan = default;

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_ISpanFormattable_TryFormat()
    {
        const string idSpanText = "Some text idSpan";
        Span<char> destination = stackalloc char[64];

        var idSpan = new Tag(idSpanText);

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten, format, formatProvider);

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void Tag_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string idSpanText = "Some text idSpan";
        Span<char> destination = stackalloc char[64];

        var idSpan = new Tag(idSpanText);

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Tag_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        Tag idSpan = default;

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("Some text idSpan").AsSpan();
        Span<byte> utf8Destination = stackalloc byte[64];

        var idSpan = new Tag("Some text idSpan");

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(idSpan.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected).Should().BeTrue();
    }

    [Fact]
    public void Tag_FromString()
    {
        var expected = new Tag("Some text idSpan");

        var result = Tag.FromString("Some text idSpan");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Tag_FromIdSpan()
    {
        var expected = new Tag("Some text idSpan");

        var result = Tag.FromIdSpan(new IdSpan("Some text idSpan"));

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Tag_ToIdSpan()
    {
        var expected = new IdSpan("Some text idSpan");

        var result = Tag.ToIdSpan(new Tag("Some text idSpan"));

        result.Should().BeEquivalentTo(expected);
    }
}
