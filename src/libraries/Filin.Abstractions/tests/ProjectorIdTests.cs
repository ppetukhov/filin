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

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.Hash.Should().Be(StableHashTests.HASH64_OF_EMPTY);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ProjectorId_Constructor_From_Null_Or_EmptyString_With_Version_0(string? id)
    {
        var result = new ProjectorId(id: id!, version: 0);

        result.IsDefault.Should().BeTrue();
        result.AsSpan().Length.Should().Be(0);
        result.Hash.Should().Be(StableHashTests.HASH64_OF_EMPTY);
    }

    [Theory]
    [MemberData(nameof(StringWithExpectedHashCode))]
    public void ProjectorId_Constructor_From_String(string id, ulong expectedHash)
    {
        var result = new ProjectorId(id: id);

        result.IsDefault.Should().BeFalse();
        result.Value.Should().Be(id);
        result.Hash.Should().Be(expectedHash);
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

        result.Value.Should().Be(txt);
        result.Hash.Should().Be(expectedHashCode);
        result.IsDefault.Should().Be(string.IsNullOrEmpty(txt));
    }

    [Fact]
    public void ProjectorId_Equals_Return_True()
    {
        var identityX = new ProjectorId(id: "Some text");
        var identityY = new ProjectorId(id: "Some text");

        var result = identityX.Equals(identityY);

        result.Should().BeTrue();
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

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "any-data")]
    [InlineData("test-value", "any-value")]
    public void ProjectorId_Equals_Return_False(string valueX, string valueY)
    {
        var identityX = new ProjectorId(valueX);
        var identityY = new ProjectorId(valueY);

        var result = identityX.Equals(identityY);

        result.Should().BeFalse();
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

        result.Should().BeFalse();
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

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ProjectorId_ToString_Return_Null_When_IsDefault()
    {
        ProjectorId identity = default;

        identity.ToString().Should().BeNull(because: "ProjectorId is default");
    }

    [Fact]
    public void ProjectorId_ToString_Return_Original_String()
    {
        var expectedString = "Some text";

        var identity = new ProjectorId(id: expectedString);

        identity.ToString().Should().Be(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectorId_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var identity = new ProjectorId(id: "Some text");

        identity.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void ProjectorId_ToString_With_Format_Return_EmptyString_When_ProjectorId_IsDefault(string? format)
    {
        ProjectorId identity = default;

        identity.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        ProjectorId identity = default;

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectorId_ISpanFormattable_TryFormat()
    {
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new ProjectorId(id: IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
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

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void ProjectorId_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string IdentityText = "Some text identity";
        Span<char> destination = stackalloc char[64];

        var identity = new ProjectorId(id: IdentityText);

        var result = identity.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(IdentityText.Length);
        destination[..charsWritten].SequenceEqual(IdentityText).Should().BeTrue();
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

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
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

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        ProjectorId identity = default;

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectorId_IUtf8SpanFormattable_TryFormat()
    {
        var expected = Encoding.UTF8.GetBytes("Some text identity").AsSpan();
        Span<byte> utf8Destination = stackalloc byte[64];

        var identity = new ProjectorId(id: "Some text identity");

        var result = identity.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(identity.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(expected).Should().BeTrue();
    }

    [Fact]
    public void ProjectorId_FromString()
    {
        var expected = new ProjectorId(id: "Some text identity");

        var result = ProjectorId.FromString("Some text identity");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ProjectorId_FromIdentity()
    {
        var expected = new ProjectorId(id: "Some text identity");

        var result = ProjectorId.FromIdSpan(new IdSpan("Some text identity"));

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ProjectorId_ToIdentity()
    {
        var expected = new IdSpan("Some text identity");

        var result = ProjectorId.ToIdSpan(new ProjectorId(id: "Some text identity"));

        result.Should().BeEquivalentTo(expected);
    }
}
