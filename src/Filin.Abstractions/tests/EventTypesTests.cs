namespace Filin.Abstractions.Tests;

public class EventTypesTests
{
    #region Constructor, IsEmpty and Count

    [Fact]
    public void Constructor_EmptyParams_ShouldCreateEmptyCollection()
    {
        // Act
        var collection = new EventTypes();

        // Assert
        Assert.True(collection.IsEmpty);
        Assert.Equal(0, collection.Count);
        Assert.True(collection.AsSpan().IsEmpty);
    }

    [Fact]
    public void Constructor_SingleElement_ShouldInitializeCorrectly()
    {
        // Arrange
        var et = new EventType("CourseSubscribed");

        // Act
        var collection = new EventTypes(et);

        // Assert
        Assert.False(collection.IsEmpty);
        Assert.Equal(1, collection.Count);
        Assert.Equal(et, collection[0]);
    }

    [Fact]
    public void Constructor_MultipleUnsortedElements_ShouldSortAndDeduplicate()
    {
        // Arrange
        var et1 = new EventType("Z_Event");
        var et2 = new EventType("A_Event");
        var et3 = new EventType("Z_Event");

        // Act
        var collection = new EventTypes(et1, et2, et3);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(et2, collection[0]); // "A_Event" (sorted)
        Assert.Equal(et1, collection[1]); // "Z_Event" (unique)
    }

    #endregion

    #region Indexer and Boundary Errors

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Indexer_InvalidIndexOnEmptyCollection_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new EventTypes();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = collection[index]);
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_WhenElementExists_ShouldReturnTrue()
    {
        // Arrange
        var et1 = new EventType("Appended");
        var et2 = new EventType("Deleted");
        var collection = new EventTypes(et2, et1);

        // Act & Assert
        Assert.True(collection.Contains(et1));
        Assert.True(collection.Contains(et2));
    }

    #endregion

    #region Add Methods (Merging)

    [Fact]
    public void Add_CollectionWithCollection_ShouldMergeSortAndDeduplicate()
    {
        // Arrange
        var c1 = new EventTypes(new EventType("1_Created"), new EventType("3_Updated"));
        var c2 = new EventTypes(new EventType("3_Updated"), new EventType("2_Deleted"));

        // Act
        EventTypes result = [.. c1, .. c2];

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new EventType("1_Created"), result[0]);
        Assert.Equal(new EventType("2_Deleted"), result[1]);
        Assert.Equal(new EventType("3_Updated"), result[2]);
    }

    [Fact]
    public void CollectionExpression_WithBracketSyntax_ShouldCompileAndSortAutomated()
    {
        // Act
        EventTypes collection = [new EventType("Updated"), new EventType("Created")];

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(new EventType("Created"), collection[0]);
        Assert.Equal(new EventType("Updated"), collection[1]);
    }

    #endregion

    #region Equality and IComparable

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var c1 = new EventTypes(new EventType("A"), new EventType("B"));
        var c2 = new EventTypes(new EventType("B"), new EventType("A"));

        // Act & Assert
        Assert.True(c1.Equals(c2));
        Assert.True(c1 == c2);
        Assert.Equal(c2.GetHashCode(), c1.GetHashCode());
    }

    #endregion

    #region Enumerator Testing

    [Fact]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new EventTypes(new EventType("A"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _ = collection.GetEnumerator().Current);
        Assert.Equal("EventTypeEnumerator.Current value has not been set.", ex.Message);
    }

    #endregion

    #region Relational Operators Tests

    [Fact]
    public void RelationalOperators_WithVariousCollections_ShouldEvaluateCorrectly()
    {
        // Arrange
        EventTypes c1 = [new EventType("Created")];
        EventTypes c2 = [new EventType("Updated")];
        EventTypes c3 = [new EventType("Created")];

        // Act & Assert
        Assert.True(c1 == c3);
        Assert.True(c1 != c2);
        Assert.True(c1 < c2);
        Assert.True(c1 <= c3);
        Assert.True(c2 > c1);
        Assert.True(c3 >= c1);
    }

    #endregion

    #region ToString and ISpanFormattable Formatting Tests

    [Fact]
    public void ToString_WhenCollectionIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var collection = new EventTypes();

        // Act & Assert
        Assert.Empty(collection.ToString());
    }

    [Fact]
    public void ToString_WithDefaultSeparator_ShouldJoinElementsWithCommaAndSpace()
    {
        // Arrange
        EventTypes collection = [new EventType("Updated"), new EventType("Created")]; // Auto-sorted

        // Act
        var result = collection.ToString();

        // Assert
        Assert.Equal("Created, Updated", result);
    }

    [Fact]
    public void TryFormat_WithSufficientBuffer_ShouldWriteCorrectlyAndReturnTrue()
    {
        // Arrange
        EventTypes collection = [new EventType("Created"), new EventType("Updated")];
        Span<char> destination = stackalloc char[32];

        // Act
        var success = collection.TryFormat(destination, out var charsWritten, default, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.True(success);
        Assert.Equal(16, charsWritten); // length of "Created, Updated"
        Assert.Equal("Created, Updated", new string(destination[..charsWritten]));
    }

    [Fact]
    public void TryFormat_WithInsufficientBuffer_ShouldReturnFalseAndCleanBytes()
    {
        // Arrange
        EventTypes collection = [new EventType("Created"), new EventType("Updated")];
        Span<char> tinyDestination = stackalloc char[5]; // Insufficient buffer bounds

        // Act
        var success = collection.TryFormat(tinyDestination, out var charsWritten, default, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        Assert.False(success);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void ToString_WithExtremeLengthExceeding256Chars_ShouldFallbackToPoolAndFormatFlawlessly()
    {
        // Arrange: 3 types * 100 chars = 300 chars (> 256 threshold)
        var longType1 = new EventType(new string('A', 100));
        var longType2 = new EventType(new string('B', 100));
        var longType3 = new EventType(new string('C', 100));
        EventTypes collection = [longType3, longType1, longType2];

        // Act
        var result = collection.ToString();

        // Assert
        Assert.Equal(304, result.Length);
        Assert.StartsWith(new string('A', 100), result, StringComparison.Ordinal);
        Assert.EndsWith(new string('C', 100), result, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_WithCustomFormatterProvider_ShouldDelegateToCustomFormatter()
    {
        // Arrange
        var mockFormatter = new LowercaseTypeFormatter();
        EventTypes collection = [new EventType("Created"), new EventType("Updated")];

        // Act
        var result = string.Format(mockFormatter, "{0}", collection);

        // Assert
        Assert.Equal("CREATED, UPDATED", result);
    }

    // Custom helper formatter implementation for explicit boundary unit tests
    sealed class LowercaseTypeFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg is EventTypes types ? types.ToString().ToUpperInvariant() : arg?.ToString() ?? string.Empty;
    }

    #endregion
}
