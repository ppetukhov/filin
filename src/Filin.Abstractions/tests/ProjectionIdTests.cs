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

        Assert.False(result.IsDefault);
        Assert.Equal(projId, result.ProjectorId);
        Assert.Equal(projKey, result.Key);
        Assert.Equal(expectedHashCode, result.GetHashCode());
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

        Assert.True(result);
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

        Assert.False(result);
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

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void ProjectionId_ToString_Return_Null_When_IsDefault()
    {
        ProjectionId idSpan = default;

        Assert.Null(idSpan.ToString());
    }

    [Fact]
    public void ProjectionId_ToString()
    {
        var expectedString = "some-proj@10/1562";

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        Assert.Equal(expectedString, idSpan.ToString());
    }

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(FormatWithFormatProviderAndExpectedString))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void ProjectionId_ToString_With_FormatProvider(string? format, IFormatProvider formatProvider, string expectedString)
    {
        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        Assert.Equal(expectedString, idSpan.ToString(format: format, formatProvider: formatProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AnyFomat")]
    public void ProjectionId_ToString_With_Format_Return_EmptyString_When_EventType_IsDefault(string? format)
    {
        ProjectionId idSpan = default;

        Assert.Empty(idSpan.ToString(format: format, formatProvider: null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectionId_ISpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<char> destination = stackalloc char[destinationCharsCount];

        ProjectionId idSpan = default;

        var result = idSpan.TryFormat(destination, out var charsWritten);

        Assert.True(result);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ProjectionId_ISpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<char> destination = stackalloc char[8];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten);

        Assert.False(result);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ProjectionId_ISpanFormattable_TryFormat()
    {
        const string idSpanText = "some-proj@10/1562";
        Span<char> destination = stackalloc char[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(destination, out var charsWritten);

        Assert.True(result);
        Assert.Equal(idSpanText.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(idSpanText));
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

        Assert.True(result);
        Assert.Equal(expectedString.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(expectedString));
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

        Assert.True(result);
        Assert.Equal(idSpanText.Length, charsWritten);
        Assert.True(destination[..charsWritten].SequenceEqual(idSpanText));
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

        Assert.False(result);
        Assert.Equal(0, charsWritten);
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

        Assert.True(result);
        Assert.Equal(expectedBytes.Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(expectedBytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat_Return_True_When_IsDefault(int destinationCharsCount)
    {
        Span<byte> utf8Destination = stackalloc byte[destinationCharsCount];

        ProjectionId idSpan = default;

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        Assert.True(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat_Return_False_When_Small_Destination_Size()
    {
        Span<byte> utf8Destination = stackalloc byte[8];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void ProjectionId_IUtf8SpanFormattable_TryFormat()
    {
        var idSpanBytes = Encoding.UTF8.GetBytes("some-proj@10/1562");
        Span<byte> utf8Destination = stackalloc byte[64];

        var idSpan = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = idSpan.TryFormat(utf8Destination, out var bytesWritten);

        Assert.True(result);
        Assert.Equal(idSpanBytes.AsSpan().Length, bytesWritten);
        Assert.True(utf8Destination[..bytesWritten].SequenceEqual(idSpanBytes.AsSpan()));
    }

    [Fact]
    public void ProjectionId_FromValueTuple()
    {
        var expected = new ProjectionId(new ProjectorId("some-proj@10"), new IdSpan("1562"));

        var result = ProjectionId.FromValueTuple((new ProjectorId("some-proj@10"), new IdSpan("1562")));

        Assert.Equal(expected, result);
    }
}
