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
        var IdSpanX = new Tag(nameX!);
        var IdSpanY = new Tag(nameY!);

        var result = IdSpanX.Equals(IdSpanY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "any-data")]
    [InlineData(null, "")]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void Tag_Equals_Return_False(string? nameX, string? nameY)
    {
        var IdSpanX = new Tag(nameX!);
        var IdSpanY = new Tag(nameY!);

        var result = IdSpanX.Equals(IdSpanY);

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
        var IdSpanX = new Tag(nameX!);
        var IdSpanY = new Tag(nameY!);

        var result = IdSpanX.CompareTo(IdSpanY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void Tag_ToString_Return_Null_When_IsDefault()
    {
        Tag IdSpan = default;

        IdSpan.ToString().Should().BeNull(because: "Tag is default");
    }

    [Fact]
    public void Tag_ToString_Return_Original_String()
    {
        var expectedString = "Some text";

        var IdSpan = new Tag(expectedString);

        IdSpan.ToString().Should().Be(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var IdSpan = new Tag("Some text");

        IdSpan.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void Tag_ToString_With_Format_Return_EmptyString_When_EventType_IsDefault(string? format)
    {
        Tag IdSpan = default;

        IdSpan.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Tag_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        Tag IdSpan = default;

        var result = IdSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_ISpanFormattable_TryFormat()
    {
        const string IdSpanText = "Some text IdSpan";
        Span<char> destination = stackalloc char[64];

        var IdSpan = new Tag(IdSpanText);

        var result = IdSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdSpanText.Length);
        destination[..charsWritten].SequenceEqual(IdSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(destination, out var charsWritten, format, formatProvider);

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
        const string IdSpanText = "Some text IdSpan";
        Span<char> destination = stackalloc char[64];

        var IdSpan = new Tag(IdSpanText);

        var result = IdSpan.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdSpanText.Length);
        destination[..charsWritten].SequenceEqual(IdSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Tag_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

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

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

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

        Tag IdSpan = default;

        var result = IdSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Tag_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("Some text IdSpan").AsSpan();
        Span<byte> utf8Destination = stackalloc byte[64];

        var IdSpan = new Tag("Some text IdSpan");

        var result = IdSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(IdSpan.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected).Should().BeTrue();
    }

    [Fact]
    public void Tag_FromString()
    {
        var expected = new Tag("Some text IdSpan");

        var result = Tag.FromString("Some text IdSpan");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Tag_FromIdSpan()
    {
        var expected = new Tag("Some text IdSpan");

        var result = Tag.FromIdSpan(new IdSpan("Some text IdSpan"));

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Tag_ToIdSpan()
    {
        var expected = new IdSpan("Some text IdSpan");

        var result = Tag.ToIdSpan(new Tag("Some text IdSpan"));

        result.Should().BeEquivalentTo(expected);
    }
}
