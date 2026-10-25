using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class IdSpanTests
{
    public static readonly TheoryData<string?, ulong> NullOrEmptyByteArraysWithExpectedHash = StableHashTests.NullOrEmptyStringWithExpectedHash;

    public static readonly TheoryData<string, ulong> TextWithExpectedHash = StableHashTests.TextWithExpectedHash;

    public static readonly TheoryData<string, ulong> StringWithExpectedHashCode = new()
    {
        { "Lorem Ipsum is simply dummy text of the printing and typesetting industry", 12843400592381000807UL },
        { "Contrary to popular belief, Lorem Ipsum is not simply random text.", 6378906660793192535UL },
        { "b0507063-77f0-48de-b005-88e35651ce30", 17738195476351242877UL },
        { @"!""#$%&'()*+,-./:;<=>?@[\]^_`~", 6822838985451514463UL }
    };

    public static readonly TheoryData<string?, ulong> UnsafeCreate_RawByteArrayWithHash = new()
    {
        { null, 111 },
        { "", 1255 },
        { "random text and random hash", 75459 }
    };

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = new()
    {
        { null, TestFormatProvider.Instance, TestFormatProvider.TestFormat(null) },
        { "", TestFormatProvider.Instance, TestFormatProvider.TestFormat("") },
        { "NUMBER", TestFormatProvider.Instance, TestFormatProvider.TestFormat("NUMBER") },
    };

    [Theory]
    [MemberData(nameof(NullOrEmptyByteArraysWithExpectedHash))]
    public void IdSpan_Constructor_From_Null_Or_EmptyByteArray(string? id, ulong expectedHash)
    {
        var result = new IdSpan(id!);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [MemberData(nameof(TextWithExpectedHash))]
    public void IdSpan_Constructor_From_ByteArray(string id, ulong expectedHash)
    {
        var expectedSpan = id.AsSpan();

        var result = new IdSpan(id);

        result.IsDefault.Should().BeFalse();
        result.Value.Should().Be(id);
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IdSpan_Constructor_From_Null_Or_EmptyString(string? id)
    {
        var result = new IdSpan(id!);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.Hash.Should().Be(StableHashTests.HASH64_OF_EMPTY);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void IdSpan_Constructor_From_String(string id, ulong expectedHash)
    {
        var expectedSpan = id.AsSpan();

        var result = new IdSpan(id);

        result.IsDefault.Should().BeFalse();
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [MemberData(nameof(UnsafeCreate_RawByteArrayWithHash))]
    public void IdSpan_UnsafeCreate_IdSpan(string? id, ulong expectedHash)
    {
        var isDefault = id is not { Length: > 0 };

#pragma warning disable CS0618 // Type or member is obsolete
        var result = IdSpan.UnsafeCreate(id!, expectedHash);
#pragma warning restore CS0618 // Type or member is obsolete

        result.IsDefault.Should().Be(isDefault);
        result.AsSpan().SequenceEqual(id.AsSpan()).Should().BeTrue();
        result.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("test-value", "test-value")]
    public void IdSpan_Equals_Return_True(string? valueX, string? valueY)
    {
        var idSpanX = new IdSpan(valueX!);
        var idSpanY = new IdSpan(valueY!);

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "any-data")]
    [InlineData("any-data", null)]
    [InlineData(null, "")]
    [InlineData("", null)]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void IdSpan_Equals_Return_False(string? valueX, string? valueY)
    {
        var idSpanX = new IdSpan(valueX!);
        var idSpanY = new IdSpan(valueY!);

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test-value")]
    public void IdSpan_Equals_Ignore_Hash(string? id)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var idSpanX = IdSpan.UnsafeCreate(id!, 111);
        var idSpanY = IdSpan.UnsafeCreate(id!, 333);
#pragma warning restore CS0618 // Type or member is obsolete

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(-1, null, "")]
    [InlineData(0, "", "")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-97, "abc", "abca")]
    [InlineData(97, "abca", "abc")]
    public void IdSpan_CompareTo(int expectedValue, string? valueX, string? valueY)
    {
        var idSpanX = new IdSpan(valueX!);
        var idSpanY = new IdSpan(valueY!);

        var result = idSpanX.CompareTo(idSpanY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void IdSpan_ToString_Return_Null_When_IsDefault()
    {
        var idSpan = (IdSpan)default;

        idSpan.ToString().Should().BeNull(because: "idSpan is default");
    }

    [Fact]
    public void IdSpan_ToString_Return_UTF8_String()
    {
        var expectedString = "Some text";

        var idSpan = new IdSpan(expectedString);

        idSpan.ToString().Should().Be(expectedString);
        idSpan.Value.Should().BeSameAs(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void IdSpan_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var idSpan = new IdSpan("Some text");

        idSpan.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void IdSpan_ToString_With_Format_Return_EmptyString_When_IdSpan_IsDefault(string? format)
    {
        IdSpan idSpan = default;

        idSpan.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void IdSpan_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        IdSpan idSpan = default;

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void IdSpan_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new IdSpan("Some text IdSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void IdSpan_ISpanFormattable_TryFormat()
    {
        const string idSpanText = "Some text IdSpan";
        Span<char> destination = stackalloc char[64];

        var idSpan = new IdSpan(idSpanText);

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void IdSpan_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var idSpan = new IdSpan("Some text IdSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten, format, formatProvider);

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void IdSpan_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string idSpanText = "Some text IdSpan";
        Span<char> destination = stackalloc char[64];

        var idSpan = new IdSpan(idSpanText);

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void IdSpan_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new IdSpan("Some text IdSpan");

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void IdSpan_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var idSpan = new IdSpan("Some text IdSpan");

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void IdSpan_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        IdSpan idSpan = default;

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void IdSpan_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var idSpan = new IdSpan("Some text IdSpan");

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void IdSpan_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("Some text IdSpan").AsSpan();
        Span<byte> utf8Destination = stackalloc byte[64];

        var idSpan = new IdSpan("Some text IdSpan");



        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(idSpan.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected).Should().BeTrue();
    }

    [Fact]
    public void IdSpan_FromString()
    {
        var expected = new IdSpan("Some text IdSpan");

        var result = IdSpan.FromString("Some text IdSpan");

        result.Should().BeEquivalentTo(expected);
    }

    /// <inheritdoc/>
    internal sealed class TestFormatProvider : IFormatProvider
    {
        internal static readonly TestFormatProvider Instance = new();

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? IdSpanCustomFormatter.Instance : null;

        /// <inheritdoc/>
        sealed class IdSpanCustomFormatter : ICustomFormatter
        {
            internal static readonly IdSpanCustomFormatter Instance = new();

            /// <inheritdoc/>
            public string Format(string? format, object? arg, IFormatProvider? formatProvider) => TestFormat(format);
        }

        public static string TestFormat(string? format) => $"Custom formated with format: '{format}'";
    }
}
