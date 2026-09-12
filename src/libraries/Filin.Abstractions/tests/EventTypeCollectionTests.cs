namespace Filin.Abstractions.Tests;

public class EventTypeCollectionTests
{
    #region Constructor, IsEmpty and Count

    [Fact]
    public void Constructor_EmptyParams_ShouldCreateEmptyCollection()
    {
        // Act
        var collection = new EventTypeCollection();

        // Assert
        collection.IsEmpty.Should().BeTrue();
        collection.Count.Should().Be(0);
        collection.AsSpan().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_SingleElement_ShouldInitializeCorrectly()
    {
        // Arrange
        var et = new EventType("CourseSubscribed");

        // Act
        var collection = new EventTypeCollection(et);

        // Assert
        collection.IsEmpty.Should().BeFalse();
        collection.Count.Should().Be(1);
        collection[0].Should().Be(et);
    }

    [Fact]
    public void Constructor_MultipleUnsortedElements_ShouldSortAndDeduplicate()
    {
        // Arrange
        var et1 = new EventType("Z_Event");
        var et2 = new EventType("A_Event");
        var et3 = new EventType("Z_Event");

        // Act
        var collection = new EventTypeCollection(et1, et2, et3);

        // Assert
        collection.Count.Should().Be(2);
        collection[0].Should().Be(et2); // "A_Event" (sorted)
        collection[1].Should().Be(et1); // "Z_Event" (unique)
    }

    #endregion

    #region Indexer and Boundary Errors

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Indexer_InvalidIndexOnEmptyCollection_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new EventTypeCollection();

        // Act
        Action act = () => _ = collection[index];

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_WhenElementExists_ShouldReturnTrue()
    {
        // Arrange
        var et1 = new EventType("Appended");
        var et2 = new EventType("Deleted");
        var collection = new EventTypeCollection(et2, et1);

        // Act & Assert
        collection.Contains(et1).Should().BeTrue();
        collection.Contains(et2).Should().BeTrue();
    }

    #endregion

    #region Add Methods (Merging)

    [Fact]
    public void Add_CollectionWithCollection_ShouldMergeSortAndDeduplicate()
    {
        // Arrange
        var c1 = new EventTypeCollection(new EventType("1_Created"), new EventType("3_Updated"));
        var c2 = new EventTypeCollection(new EventType("3_Updated"), new EventType("2_Deleted"));

        // Act
        EventTypeCollection result = [.. c1, .. c2];

        // Assert
        result.Count.Should().Be(3);
        result[0].Should().Be(new EventType("1_Created"));
        result[1].Should().Be(new EventType("2_Deleted"));
        result[2].Should().Be(new EventType("3_Updated"));
    }

    [Fact]
    public void CollectionExpression_WithBracketSyntax_ShouldCompileAndSortAutomated()
    {
        // Act
        EventTypeCollection collection = [new EventType("Updated"), new EventType("Created")];

        // Assert
        collection.Count.Should().Be(2);
        collection[0].Should().Be(new EventType("Created"));
        collection[1].Should().Be(new EventType("Updated"));
    }

    #endregion

    #region Equality and IComparable

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var c1 = new EventTypeCollection(new EventType("A"), new EventType("B"));
        var c2 = new EventTypeCollection(new EventType("B"), new EventType("A"));

        // Act & Assert
        c1.Equals(c2).Should().BeTrue();
        (c1 == c2).Should().BeTrue();
        c1.GetHashCode().Should().Be(c2.GetHashCode());
    }

    #endregion

    #region Enumerator Testing

    [Fact]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new EventTypeCollection(new EventType("A"));
        var enumerator = collection.GetEnumerator();
        Exception? ex = null;

        // Act
        try
        {
            _ = enumerator.Current;
        }
        catch (Exception e)
        {
            ex = e;
        }
        finally
        {
            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();
            ex.Message.Should().Be("EventTypeCollection.Enumerator.Current value has not been set.");
        }
    }

    #endregion

    #region Relational Operators Tests

    [Fact]
    public void RelationalOperators_WithVariousCollections_ShouldEvaluateCorrectly()
    {
        // Arrange
        EventTypeCollection c1 = [new EventType("Created")];
        EventTypeCollection c2 = [new EventType("Updated")];
        EventTypeCollection c3 = [new EventType("Created")];

        // Act & Assert
        (c1 == c3).Should().BeTrue();
        (c1 != c2).Should().BeTrue();
        (c1 < c2).Should().BeTrue();
        (c1 <= c3).Should().BeTrue();
        (c2 > c1).Should().BeTrue();
        (c3 >= c1).Should().BeTrue();
    }

    #endregion

    #region ToString and ISpanFormattable Formatting Tests

    [Fact]
    public void ToString_WhenCollectionIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var collection = new EventTypeCollection();

        // Act & Assert
        collection.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ToString_WithDefaultSeparator_ShouldJoinElementsWithCommaAndSpace()
    {
        // Arrange
        EventTypeCollection collection = [new EventType("Updated"), new EventType("Created")]; // Auto-sorted

        // Act
        string result = collection.ToString();

        // Assert
        result.Should().Be("Created, Updated", "event types must follow proper sorting and default spacing layout rules");
    }

    [Fact]
    public void TryFormat_WithSufficientBuffer_ShouldWriteCorrectlyAndReturnTrue()
    {
        // Arrange
        EventTypeCollection collection = [new EventType("Created"), new EventType("Updated")];
        Span<char> destination = stackalloc char[32];

        // Act
        bool success = collection.TryFormat(destination, out int charsWritten, default, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        success.Should().BeTrue();
        charsWritten.Should().Be(16); // length of "Created, Updated"
        new string(destination[..charsWritten]).Should().Be("Created, Updated");
    }

    [Fact]
    public void TryFormat_WithInsufficientBuffer_ShouldReturnFalseAndCleanBytes()
    {
        // Arrange
        EventTypeCollection collection = [new EventType("Created"), new EventType("Updated")];
        Span<char> tinyDestination = stackalloc char[5]; // Insufficient buffer bounds

        // Act
        bool success = collection.TryFormat(tinyDestination, out int charsWritten, default, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        success.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ToString_WithExtremeLengthExceeding256Chars_ShouldFallbackToPoolAndFormatFlawlessly()
    {
        // Arrange: 3 types * 100 chars = 300 chars (> 256 threshold)
        var longType1 = new EventType(new string('A', 100));
        var longType2 = new EventType(new string('B', 100));
        var longType3 = new EventType(new string('C', 100));
        EventTypeCollection collection = [longType3, longType1, longType2];

        // Act
        string result = collection.ToString();

        // Assert
        result.Length.Should().Be(304); // 300 elements chars + 4 separator chars (", " * 2)
        result.StartsWith(new string('A', 100)).Should().BeTrue();
        result.EndsWith(new string('C', 100)).Should().BeTrue();
    }

    [Fact]
    public void ToString_WithCustomFormatterProvider_ShouldDelegateToCustomFormatter()
    {
        // Arrange
        var mockFormatter = new LowercaseTypeFormatter();
        EventTypeCollection collection = [new EventType("Created"), new EventType("Updated")];

        // Act
        string result = string.Format(mockFormatter, "{0}", collection);

        // Assert
        result.Should().Be("created, updated", "the collection formatting must cede evaluation execution to custom providers if active");
    }

    // Custom helper formatter implementation for explicit boundary unit tests
    sealed class LowercaseTypeFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (arg is EventTypeCollection types)
            {
                return types.ToString().ToLowerInvariant();
            }
            return arg?.ToString() ?? string.Empty;
        }
    }

    #endregion
}
