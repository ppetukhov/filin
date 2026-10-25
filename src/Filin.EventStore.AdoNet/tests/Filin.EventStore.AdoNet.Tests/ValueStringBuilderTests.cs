using Filin.Text;

using Xunit;

namespace Filin.EventStore.AdoNet.Tests;

public class ValueStringBuilderTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        Span<char> buffer = stackalloc char[10];
        var sb = new ValueStringBuilder(buffer);

        // Assert
        Assert.Equal(0, sb.Length);
        Assert.True(sb.AsSpan().IsEmpty);
    }

    [Fact]
    public void Append_Char_WithinCapacity_AddsChar()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append('A');
        sb.Append('B');

        // Assert
        Assert.Equal(2, sb.Length);
        Assert.Equal("AB", sb.ToString());
    }

    [Fact]
    public void Append_Char_ExceedingCapacity_GrowsBuffer()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append('A');
        sb.Append('B');
        sb.Append('C');

        // Assert
        Assert.Equal(3, sb.Length);
        Assert.Equal("ABC", sb.ToString());
    }

    [Fact]
    public void Append_String_Null_DoesNothing()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append((string?)null);

        // Assert
        Assert.Equal(0, sb.Length);
    }

    [Fact]
    public void Append_String_LengthOne_WithinCapacity_AddsChar()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append("X");

        // Assert
        Assert.Equal(1, sb.Length);
        Assert.Equal("X", sb.ToString());
    }

    [Fact]
    public void Append_String_MultiChar_GrowsIfNecessary()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var sb = new ValueStringBuilder(buffer);

        // Act
        sb.Append("Hello");

        // Assert
        Assert.Equal(5, sb.Length);
        Assert.Equal("Hello", sb.ToString());
    }

    [Fact]
    public void Append_Span_AddsContentCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var sb = new ValueStringBuilder(buffer);
        ReadOnlySpan<char> text = "TestSpan".AsSpan();

        // Act
        sb.Append(text);

        // Assert
        Assert.Equal(8, sb.Length);
        Assert.Equal("TestSpan", sb.ToString());
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSlice()
    {
        // Arrange
        Span<char> buffer = stackalloc char[10];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Data");

        // Act
        ReadOnlySpan<char> span = sb.AsSpan();

        // Assert
        Assert.Equal(4, span.Length);
        Assert.Equal("Data", span.ToString());
    }

    [Fact]
    public void Length_Set_ChangesPosition()
    {
        // Arrange
        Span<char> buffer = stackalloc char[10];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Hello");

        // Act
        sb.Length = 3;

        // Assert
        Assert.Equal(3, sb.Length);
        Assert.Equal("Hel", sb.ToString());
    }

    [Fact]
    public void Dispose_ClearsBuilderAndReturnsPoolArray()
    {
        // Arrange
        Span<char> buffer = stackalloc char[1];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Trigger ArrayPool Rent");

        // Act
        sb.Dispose();

        // Assert
        Assert.Equal(0, sb.Length);
        Assert.True(sb.AsSpan().IsEmpty);
    }

    [Fact]
    public void Length_Set_ToValidBoundary_Success()
    {
        // Arrange
        Span<char> buffer = stackalloc char[4];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("ABCD");

        // Act & Assert
        sb.Length = 4;
        sb.Length = 0;

        Assert.Equal(0, sb.Length);
    }

    [Fact]
    public void Append_EmptyString_DoesNotAlterState()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("A");

        // Act
        sb.Append(string.Empty);
        sb.Append(ReadOnlySpan<char>.Empty);

        // Assert
        Assert.Equal(1, sb.Length);
        Assert.Equal("A", sb.ToString());
    }

    [Fact]
    public void Append_HugeString_CausesGrowCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[1];
        var sb = new ValueStringBuilder(buffer);
        string longStr = new string('X', 1000);

        // Act
        sb.Append(longStr);

        // Assert
        Assert.Equal(1000, sb.Length);
        Assert.Equal(longStr, sb.ToString());
    }

    [Fact]
    public void Append_MultipleGrows_WorksConsistently()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var sb = new ValueStringBuilder(buffer);

        // Act & Assert
        sb.Append("AB");
        sb.Append("C");
        sb.Append("DEF");

        Assert.Equal(6, sb.Length);
        Assert.Equal("ABCDEF", sb.ToString());
    }

    [Fact]
    public void UseAfterDispose_SafeDefaultBehavior()
    {
        // Arrange
        Span<char> buffer = stackalloc char[4];
        var sb = new ValueStringBuilder(buffer);
        sb.Append("Test");
        sb.Dispose();

        // Act
        sb.Append('A');

        // Assert
        Assert.Equal(1, sb.Length);
        Assert.Equal("A", sb.ToString());
    }

    [Fact]
    public void Length_Set_NegativeValue_ThrowsInDebug()
    {
        Span<char> buffer = stackalloc char[4];
        var sb = new ValueStringBuilder(buffer);

        bool asserted = false;
        try
        {
            sb.Length = -1;
        }
        catch (Exception)
        {
            asserted = true;
        }

        Assert.True(asserted);
    }

    [Fact]
    public void Length_Set_ExceedingCapacity_ThrowsInDebug()
    {
        Span<char> buffer = stackalloc char[4];
        var sb = new ValueStringBuilder(buffer);

        bool asserted = false;
        try
        {
            sb.Length = 5;
        }
        catch (Exception)
        {
            asserted = true;
        }

        Assert.True(asserted);
    }
}
