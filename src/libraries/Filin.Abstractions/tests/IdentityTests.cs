using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class IdentityTests
{
    public static readonly TheoryData<byte[]?, int> NullOrEmptyByteArraysWithExpectedHash = StableHashTests.NullOrEmptyByteArraysWithExpectedHash;

    public static readonly TheoryData<byte[], int> ByteArrayWithExpectedHash = StableHashTests.ByteArrayWithExpectedHash;

    public static readonly TheoryData<string, int> StringWithExpectedHashCode = new()
    {
        { "Lorem Ipsum is simply dummy text of the printing and typesetting industry", 2087748517 },
        { "Contrary to popular belief, Lorem Ipsum is not simply random text.", 332455997 },
        { "b0507063-77f0-48de-b005-88e35651ce30", 1931022079 },
        { @"!""#$%&'()*+,-./:;<=>?@[\]^_`~", -1771569236 }
    };

    public static readonly TheoryData<byte[]?, int> UnsafeCreate_RawByteArrayWithHash = new()
    {
        { null, 111 },
        { [], 1255 },
        { [1, 2, 3, 4], 75459 }
    };

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = new()
    {
        { null, TestFormatProvider.Instance, TestFormatProvider.TestFormat(null) },
        { "", TestFormatProvider.Instance, TestFormatProvider.TestFormat("") },
        { "NUMBER", TestFormatProvider.Instance, TestFormatProvider.TestFormat("NUMBER") },
    };

    [Theory]
    [MemberData(nameof(NullOrEmptyByteArraysWithExpectedHash))]
    public void Identity_Constructor_From_Null_Or_EmptyByteArray(byte[]? value, int expectedHash)
    {
        var result = new Identity(value);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.GetHashCode().Should().Be(expectedHash);
    }

    [Theory]
    [MemberData(nameof(ByteArrayWithExpectedHash))]
    public void Identity_Constructor_From_ByteArray(byte[] value, int expectedHash)
    {
        var expectedSpan = value.AsSpan();

        var result = new Identity(value);

        result.IsDefault.Should().BeFalse();
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.GetHashCode().Should().Be(expectedHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Identity_Constructor_From_Null_Or_EmptyString(string? id)
    {
        var result = new Identity(id);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.GetHashCode().Should().Be(StableHashTests.HASHCODE_OF_EMPTY);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void Identity_Constructor_From_String(string value, int expectedHashCode)
    {
        var expectedSpan = Encoding.UTF8.GetBytes(value).AsSpan();

        var result = new Identity(value);

        result.IsDefault.Should().BeFalse();
        result.AsSpan().SequenceEqual(expectedSpan).Should().BeTrue();
        result.GetHashCode().Should().Be(expectedHashCode);
    }

    [Theory]
    [MemberData(nameof(UnsafeCreate_RawByteArrayWithHash))]
    public void Identity_UnsafeCreate_Identity(byte[]? value, int hashCode)
    {
        var isDefault = value is not { Length: > 0 };

        var result = Identity.UnsafeCreate(value, hashCode);

        result.IsDefault.Should().Be(isDefault);
        result.AsSpan().SequenceEqual(value.AsSpan()).Should().BeTrue();
        result.GetHashCode().Should().Be(hashCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("test-value", "test-value")]
    public void Identity_Equals_Return_True(string? valueX, string? valueY)
    {
        var identityX = new Identity(valueX);
        var identityY = new Identity(valueY);

        var result = identityX.Equals(identityY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "any-data")]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void Identity_Equals_Return_False(string? valueX, string? valueY)
    {
        var identityX = new Identity(valueX);
        var identityY = new Identity(valueY);

        var result = identityX.Equals(identityY);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test-value")]
    public void Identity_Equals_Ignore_HashCode(string? value)
    {
        var rawBytes = value is not null ? Encoding.UTF8.GetBytes(value) : null;

        var identityX = Identity.UnsafeCreate(rawBytes, 111);
        var identityY = Identity.UnsafeCreate(rawBytes, 333);

        var result = identityX.Equals(identityY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(0, null, "")]
    [InlineData(0, "", "")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-1, "abc", "abca")]
    [InlineData(1, "abca", "abc")]
    public void Identity_CompareTo(int expectedValue, string? valueX, string? valueY)
    {
        var identityX = new Identity(valueX);
        var identityY = new Identity(valueY);

        var result = identityX.CompareTo(identityY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void Identity_ToString_Return_Null_When_IsDefault()
    {
        var identity = (Identity)default;

        identity.ToString().Should().BeNull(because: "Identity is default");
    }

    [Fact]
    public void Identity_ToString_Return_UTF8_String()
    {
        var expectedString = "Some text";

        var rawBytes = Encoding.UTF8.GetBytes(expectedString);

        var identity = new Identity(rawBytes);

        identity.ToString().Should().Be(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Identity_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var identity = new Identity("Some text");

        identity.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void Identity_ToString_With_Format_Return_EmptyString_When_Identity_IsDefault(string? format)
    {
        Identity identity = default;

        identity.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Identity_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        Identity identity = default;

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Identity_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void Identity_ISpanFormattable_TryFormat()
    {
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new Identity(IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Identity_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten, format, formatProvider);

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void Identity_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new Identity(IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Identity_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void Identity_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Identity_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        Identity identity = default;

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Identity_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void Identity_IUtf8SpanFormattable_TryFormat()
    {
        Span<byte> utf8Destination = stackalloc byte[64];

        var identity = new Identity("Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(identity.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(identity.AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void Identity_FromString()
    {
        var expected = new Identity("Some text identity");

        var result = Identity.FromString("Some text identity");

        result.Should().BeEquivalentTo(expected);
    }

    /// <inheritdoc/>
    internal sealed class TestFormatProvider : IFormatProvider
    {
        internal static readonly TestFormatProvider Instance = new TestFormatProvider();

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? IdentityCustomFormatter.Instance : null;

        /// <inheritdoc/>
        sealed class IdentityCustomFormatter : ICustomFormatter
        {
            internal static readonly IdentityCustomFormatter Instance = new IdentityCustomFormatter();

            /// <inheritdoc/>
            public string Format(string? format, object? arg, IFormatProvider? formatProvider) => TestFormat(format);
        }

        public static string TestFormat(string? format) => $"Custom formated with format: '{format}'";
    }
}
