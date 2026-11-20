using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class ProjectorIdTests
{
    public static readonly TheoryData<string, ulong> StringWithExpectedHashCode = IdSpanTests.StringWithExpectedHashCode;

    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = IdSpanTests.FormatWithFormatProviderAndExpectedString;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ProjectorId_Constructor_From_Null_Or_EmptyString(string? id)
    {
        var result = new ProjectorId(id: id!);

        Assert.True(result.IsDefault);
        Assert.Equal(0, result.AsSpan().Length);
        Assert.Equal(StableHashTests.HASH64_OF_EMPTY, result.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ProjectorId_Constructor_From_Null_Or_EmptyString_With_Version_0(string? id)
    {
        var result = new ProjectorId(id: id!, version: 0);

        Assert.True(result.IsDefault);
        Assert.Equal(0, result.AsSpan().Length);
        Assert.Equal(StableHashTests.HASH64_OF_EMPTY, result.Hash);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void ProjectorId_Constructor_From_String(string id, ulong expectedHash)
    {
        var result = new ProjectorId(id: id);

        Assert.False(result.IsDefault);
        Assert.Equal(id, result.Value);
        Assert.Equal(expectedHash, result.Hash);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("", 1)]
    [InlineData("some-projector", 0)]
    [InlineData("some-projector", 1)]
    public void ProjectorId_Constructor_From_String_With_Version(string id, uint version)
    {
        var txt = version is 0 ? id : $"{id}@{version}";
        var expectedHashCode = StableHash.Hash64(txt);

        var result = new ProjectorId(id: id, version: version);

        Assert.Equal(txt, result.Value);
        Assert.Equal(expectedHashCode, result.Hash);
        Assert.Equal(string.IsNullOrEmpty(txt), result.IsDefault);
    }

    [Fact]
    public void ProjectorId_Equals_Return_True()
    {
        var identityX = new ProjectorId(id: "Some text");
        var identityY = new ProjectorId(id: "Some text");

        var result = identityX.Equals(identityY);

        Assert.True(result);
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("@12", "", 12)]
    [InlineData("test-value", "test-value", 0)]
    [InlineData("test-value@111", "test-value", 111)]
    public void ProjectorId_Equals_Return_True_With_Version(string valueX, string valueY, uint versionY)
    {
        var identityX = new ProjectorId(id: valueX);
        var identityY = new ProjectorId(id: valueY, version: versionY);

        var result = identityX.Equals(identityY);

        Assert.True(result);
    }

    [Theory]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void ProjectorId_Equals_Return_False(string valueX, string valueY)
    {
        var identityX = new ProjectorId(valueX);
        var identityY = new ProjectorId(valueY);

        var result = identityX.Equals(identityY);

        Assert.False(result);
    }

    [Theory]
    [InlineData("", "", 1)]
    [InlineData("@12", "", 10)]
    [InlineData("test-value", "test-value", 2)]
    [InlineData("test-value@1", "test-value", 0)]
    public void ProjectorId_Equals_Return_False_With_Version(string valueX, string valueY, uint versionY)
    {
        var identityX = new ProjectorId(id: valueX);
        var identityY = new ProjectorId(id: valueY, version: versionY);

        var result = identityX.Equals(identityY);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0, "", "")]
    [InlineData(0, "abc", "abc")]
    [InlineData(-23, "abc", "abz")]
    [InlineData(23, "abz", "abc")]
    [InlineData(-97, "abc", "abca")]
    [InlineData(97, "abca", "abc")]
    [InlineData(-64, "abc", "abc@2")]
    [InlineData(-1, "abc@1", "abc@20")]
    [InlineData(64, "abc@2", "abc")]
    [InlineData(1, "abc@20", "abc@1")]
    public void ProjectorId_CompareTo(int expectedValue, string valueX, string valueY)
    {
        var identityX = new ProjectorId(id: valueX);
        var identityY = new ProjectorId(id: valueY);

        var result = identityX.CompareTo(identityY);

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ProjectorId_ToString_Return_Null_When_IsDefault()
    {
        ProjectorId identity = default;

        Assert.Null(identity.ToString());
    }

    [Fact]
    public void ProjectorId_ToString_Return_Original_String()
    {
        var expectedString = "Some text";

        var identity = new ProjectorId(id: expectedString);

        Assert.Equal(expectedString, identity.ToString());
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectorId_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var identity = new ProjectorId(id: "Some text");

        Assert.Equal(expectedString, identity.ToString(format: format, formatProvider: formatProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void ProjectorId_ToString_With_Format_Return_EmptyString_When_ProjectorId_IsDefault(string? format)
    {
        ProjectorId identity = default;

        Assert.Empty(identity.ToString(format: format, formatProvider: null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        ProjectorId identity = default;

        var result = identity.TryFormat(destination, out var charsWritten);

        Assert.True(result);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten);

        Assert.False(result);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ProjectorId_ISpanFormattable_TryFormat()
    {
        const string identityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new ProjectorId(id: identityText);

        var result = identity.TryFormat(destination, out var charsWritten);

        Assert.True(result);
        Assert.Equal(identityText.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(identityText));
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectorId_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten, format, formatProvider);

        Assert.True(result);
        Assert.Equal(expectedString.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(expectedString));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string identityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new ProjectorId(id: identityText);

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: null);

        Assert.True(result);
        Assert.Equal(identityText.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(identityText));
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectorId_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        Assert.False(result);
        Assert.Equal(0, charsWritten);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectorId_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        Assert.True(result);
        Assert.Equal(expectedBytes.Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(expectedBytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        ProjectorId identity = default;

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        Assert.True(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("Some text identity").AsSpan();
        Span<byte> utf8Destination = stackalloc byte[64];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        Assert.True(result);
        Assert.Equal(identity.AsSpan().Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(expected));
    }

    [Fact]
    public void ProjectorId_FromString()
    {
        var expected = new ProjectorId(id: "Some text identity");

        var result = ProjectorId.FromString("Some text identity");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorId_FromIdentity()
    {
        var expected = new ProjectorId(id: "Some text identity");

        var result = ProjectorId.FromIdSpan(new IdSpan("Some text identity"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProjectorId_ToIdentity()
    {
        var expected = new IdSpan("Some text identity");

        var result = ProjectorId.ToIdSpan(new ProjectorId(id: "Some text identity"));

        Assert.Equal(expected, result);
    }
}
