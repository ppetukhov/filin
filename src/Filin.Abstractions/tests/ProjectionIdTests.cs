using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class ProjectionIdTests
{
    public static readonly TheoryData<string?, IFormatProvider, string> FormatWithFormatProviderAndExpectedString = IdSpanTests.FormatWithFormatProviderAndExpectedString;

    [Theory]
    [InlineData("some-projector@1", "1236")]
    [InlineData("some-projector", "")]
    [InlineData("some-projector", null)]
    [InlineData("", "123456")]
    [InlineData(null, "123456")]
    public void ProjectionId_Constructor(string? projectorId, string? key)
    {
        var projId = new ProjectorId(projectorId!);
        var projKey = new IdSpan(key!);

        var expectedHashCode = HashCode.Combine(projId, projKey);

        var result = new ProjectionId(projId, projKey);

        result.IsDefault.Should().BeFalse();
        result.ProjectorId.Should().BeEquivalentTo(projId);
        result.Key.Should().BeEquivalentTo(projKey);
        result.GetHashCode().Should().Be(expectedHashCode);
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData(null, "", null, "")]
    [InlineData("", "", "", "")]
    [InlineData("test-value", "42", "test-value", "42")]
    [InlineData("test-value", null, "test-value", null)]
    public void Tag_Equals_Return_True(string? projectorIdX, string? keyX, string? projectorIdY, string? keyY)
    {
        var idSpanX = new ProjectionId(new ProjectorId(projectorIdX!), new IdSpan(keyX!));
        var idSpanY = new ProjectionId(new ProjectorId(projectorIdY!), new IdSpan(keyY!));

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null, "any-data", null)]
    [InlineData(null, null, "", "")]
    [InlineData("", "", "any-data", null)]
    [InlineData(null, "", "any-data", "")]
    [InlineData("test-value", "42", "any-value", "42")]
    [InlineData("test-value", null, "any-value", null)]
    public void Tag_Equals_Return_False(string? projectorIdX, string? keyX, string? projectorIdY, string? keyY)
    {
        var idSpanX = new ProjectionId(new ProjectorId(projectorIdX!), new IdSpan(keyX!));
        var idSpanY = new ProjectionId(new ProjectorId(projectorIdY!), new IdSpan(keyY!));

        var result = idSpanX.Equals(idSpanY);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, null, null, null, null)]
    [InlineData(-1, null, null, "", "")]
    [InlineData(0, null, "", null, "")]
    [InlineData(0, "", "", "", "")]
    [InlineData(0, "abc", "1", "abc", "1")]
    [InlineData(-23, "abc", "1", "abz", "1")]
    [InlineData(23, "abz", "1", "abc", "1")]
    [InlineData(-97, "abc", "1", "abca", "1")]
    [InlineData(97, "abca", "1", "abc", "1")]
    [InlineData(-7, "abc", "1", "abc", "8")]
    [InlineData(7, "abc", "8", "abc", "1")]
    public void ProjectionId_CompareTo(int expectedValue, string? projectorIdX, string? keyX, string? projectorIdY, string? keyY)
    {
        var idSpanX = new ProjectionId(new ProjectorId(projectorIdX!), new IdSpan(keyX!));
        var idSpanY = new ProjectionId(new ProjectorId(projectorIdY!), new IdSpan(keyY!));

        var result = idSpanX.CompareTo(idSpanY);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ProjectionId_ToString_Return_Null_When_IsDefault()
    {
        ProjectionId idSpan = default;

        idSpan.ToString().Should().BeNull(because: "ProjectionId is default");
    }

    [Fact]
    public void ProjectionId_ToString()
    {
        var expectedString = "some-proj@10/1562";

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        idSpan.ToString().Should().Be(expectedString);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectionId_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        idSpan.ToString(format: format, formatProvider: formatProvider).Should().Be(expectedString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void ProjectionId_ToString_With_Format_Return_EmptyString_When_EventType_IsDefault(string? format)
    {
        ProjectionId idSpan = default;

        idSpan.ToString(format: format, formatProvider: null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectionId_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        ProjectionId idSpan = default;

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectionId_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectionId_ISpanFormattable_TryFormat()
    {
        const string idSpanText = "some-proj@10/1562";
        Span<char> destination = stackalloc char[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectionId_ISpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<char> destination = stackalloc char[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten, format, formatProvider);

        result.Should().BeTrue();
        charsWritten.Should().Be(expectedString.Length);
        destination[..charsWritten].SequenceEqual(expectedString).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFormat")]
    public void ProjectionId_ISpanFormattable_TryFormat_Return_True_With_Format_Return_True(string? format)
    {
        const string idSpanText = "some-proj@10/1562";
        Span<char> destination = stackalloc char[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: null);

        result.Should().BeTrue();
        charsWritten.Should().Be(idSpanText.Length);
        destination[..charsWritten].SequenceEqual(idSpanText).Should().BeTrue();
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectionId_ISpanFormattable_TryFormat_Return_False_With_Format_Return_False_When_Small_Destination_Size(string? format, IFormatProvider formatProvider, string _)
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten, format: format, provider: formatProvider);

        result.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectionId_IUtf8SpanFormattable_TryFormat_Return_True_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        Span<byte> utf8Destination = stackalloc byte[128];
        var expectedBytes = Encoding.UTF8.GetBytes(expectedString);

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten, format, formatProvider);

        result.Should().BeTrue();
        bytesWritten.Should().Be(expectedBytes.Length);
        utf8Destination[..bytesWritten].SequenceEqual(expectedBytes).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        ProjectionId idSpan = default;

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    [Fact]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat()
    {
        var idSpanBytes = Encoding.UTF8.GetBytes("some-proj@10/1562");
        Span<byte> utf8Destination = stackalloc byte[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        result.Should().BeTrue();
        bytesWritten.Should().Be(idSpanBytes.AsSpan().Length);
        utf8Destination[..bytesWritten].SequenceEqual(idSpanBytes.AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void ProjectionId_FromValueTuple()
    {
        var expected = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = ProjectionId.FromValueTuple((new ProjectorId("some-proj@10"), new IdSpan("1562")));

        result.Should().BeEquivalentTo(expected);
    }
}
