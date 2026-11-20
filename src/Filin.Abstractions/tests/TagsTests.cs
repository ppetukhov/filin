using System.Globalization;

namespace Filin.Abstractions.Tests;

public class TagsTests
{
    #region Constructor, IsEmpty and Count

    [Fact]
    public void Constructor_EmptyParams_ShouldCreateEmptyCollection()
    {
        // Act
        var collection = new Tags();

        // Assert
        Assert.True(collection.IsEmpty);
        Assert.Equal(0, collection.Count);
        Assert.True(collection.AsSpan().IsEmpty);
    }

    [Fact]
    public void Constructor_SingleElement_ShouldInitializeCorrectly()
    {
        // Arrange
        var tag = new Tag("course:777");

        // Act
        var collection = new Tags(tag);

        // Assert
        Assert.False(collection.IsEmpty);
        Assert.Equal(1, collection.Count);
        Assert.Equal(tag, collection[0]);
    }

    [Fact]
    public void Constructor_UnsortedElements_ShouldSortAndDeduplicate()
    {
        // Arrange
        var t1 = new Tag("b_tag");
        var t2 = new Tag("a_tag");
        var t3 = new Tag("b_tag"); // Duplicate

        // Act
        var collection = new Tags(t1, t2, t3);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(t2, collection[0]); // "a_tag" (sorted)
        Assert.Equal(t1, collection[1]); // "b_tag" (unique)
    }

    #endregion

    #region Indexer and Boundary Errors

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Indexer_InvalidIndexOnEmptyCollection_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new Tags();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = collection[index]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-5)]
    public void Indexer_IndexOutOfBounds_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new Tags(new Tag("t1"), new Tag("t2"));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = collection[index]);
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_OnEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var collection = new Tags();
        var targetTag = new Tag("course:777");

        // Act
        var result = collection.Contains(targetTag);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Contains_WhenTagExists_ShouldReturnTrue()
    {
        // Arrange
        var t1 = new Tag("a_tag");
        var t2 = new Tag("b_tag");
        var t3 = new Tag("c_tag");
        var collection = new Tags(t2, t1, t3);

        // Act & Assert
        Assert.True(collection.Contains(t1));
        Assert.True(collection.Contains(t2));
        Assert.True(collection.Contains(t3));
    }

    #endregion

    #region Add Methods (Merging)

    [Fact]
    public void Add_CollectionWithCollection_ShouldMergeSortAndDeduplicate()
    {
        // Arrange
        var c1 = new Tags(new Tag("1_Created"), new Tag("3_Updated"));
        var c2 = new Tags(new Tag("3_Updated"), new Tag("2_Deleted"));

        // Act
        Tags result = [.. c1, .. c2];

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new Tag("1_Created"), result[0]);
        Assert.Equal(new Tag("2_Deleted"), result[1]);
        Assert.Equal(new Tag("3_Updated"), result[2]);
    }

    [Fact]
    public void Add_WithCollectionExpression_ShouldMergeCorrectlyViaCollectionBuilder()
    {
        // Arrange
        var collection = new Tags(new Tag("b"));

        // Act
        Tags result = [.. collection, new Tag("b"), new Tag("a")];

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new Tag("a"), result[0]);
        Assert.Equal(new Tag("b"), result[1]);
    }

    #endregion

    #region Equality and IComparable

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var c1 = new Tags(new Tag("a"), new Tag("b"));
        var c2 = new Tags(new Tag("b"), new Tag("a"));

        // Act & Assert
        Assert.True(c1.Equals(c2));
        Assert.True(c1 == c2);
        Assert.Equal(c2.GetHashCode(), c1.GetHashCode());
    }

    [Fact]
    public void CompareTo_DifferentElements_ShouldFollowLexicographicalOrder()
    {
        // Arrange
        var c1 = new Tags(new Tag("a"), new Tag("b"));
        var c2 = new Tags(new Tag("a"), new Tag("c"));

        // Act & Assert
        Assert.True(c1.CompareTo(c2) < 0);
        Assert.True(c2.CompareTo(c1) > 0);
    }

    #endregion

    #region Enumerator Testing

    [Fact]
    public void Enumerator_ForEachLoop_ShouldIterateInCorrectOrder()
    {
        // Arrange
        var collection = new Tags(new Tag("z"), new Tag("y"));
        var iterated = new List<string?>();

        // Act
        foreach (var tag in collection)
        {
            iterated.Add(tag.Value);
        }

        // Assert
        Assert.Equal(2, iterated.Count);
        Assert.Equal("y", iterated[0]);
        Assert.Equal("z", iterated[1]);
    }

    [Fact]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new Tags(new Tag("A"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _ = collection.GetEnumerator().Current);
        Assert.Equal("TagsEnumerator.Current value has not been set.", ex.Message);
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_WhenTagDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var t1 = new Tag("a_tag");
        var t2 = new Tag("c_tag");
        var collection = new Tags(t1, t2);
        var missingInBetween = new Tag("b_tag"); // Sits alphabetically between 'a' and 'c'
        var completelyNewTag = new Tag("z_tag"); // Sits outside the array bounds

        // Act & Assert
        Assert.False(collection.Contains(missingInBetween));
        Assert.False(collection.Contains(completelyNewTag));
    }

    [Fact]
    public void Contains_WithSingleElement_ShouldWorkCorrectly()
    {
        // Arrange
        var tag = new Tag("course:777");
        var collection = new Tags(tag);
        var wrongTag = new Tag("course:888");

        // Act & Assert
        Assert.True(collection.Contains(tag));
        Assert.False(collection.Contains(wrongTag));
    }

    #endregion

    #region Relational Operators Tests

    [Fact]
    public void RelationalOperators_WithVariousCollections_ShouldEvaluateCorrectly()
    {
        // Arrange
        Tags c1 = [new Tag("a"), new Tag("b")];
        Tags c2 = [new Tag("a"), new Tag("c")];
        Tags c3 = [new Tag("a"), new Tag("b")];

        // Act & Assert
        Assert.True(c1 == c3);
        Assert.True(c1 != c2);
        Assert.True(c1 < c2, "['a', 'b'] precedes ['a', 'c'] lexicographically");
        Assert.True(c1 <= c3);
        Assert.True(c2 > c1);
        Assert.True(c3 >= c1);
    }

    #endregion

    #region ISpanFormattable Tests

    [Fact]
    public void TryFormat_WithSufficientBuffer_ShouldWriteCorrectlyAndReturnTrue()
    {
        // Arrange
        Tags collection = [new Tag("course:777"), new Tag("student:123")];
        Span<char> destination = stackalloc char[50];

        // Act
        var success = collection.TryFormat(destination, out var charsWritten, default, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(success);
        Assert.Equal(23, charsWritten); // length of "course:777, student:123"
        Assert.Equal("course:777, student:123", new string(destination[..charsWritten]));
    }

    [Fact]
    public void TryFormat_WithInsufficientBuffer_ShouldReturnFalseAndNotCrash()
    {
        // Arrange
        Tags collection = [new Tag("course:777"), new Tag("student:123")];
        Span<char> tinyDestination = stackalloc char[15]; // Not enough space for full collection

        // Act
        var success = collection.TryFormat(tinyDestination, out var charsWritten, default, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(success, "the formatting layout must abort cleanly if the target block span is too small");
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void StringInterpolation_DirectUse_ShouldInvokeSpanFormattableNatively()
    {
        // Arrange
        Tags collection = [new Tag("a"), new Tag("b")];

        // Act
        var message = $"Boundaries: {collection}";

        // Assert
        Assert.Equal("Boundaries: a, b", message);
    }

    #endregion

    #region ICustomFormatter Integration Tests

    [Fact]
    public void ToString_WithCustomFormatterProvider_ShouldDelegeteToCustomFormatter()
    {
        // Arrange
        var mockFormatter = new UppercaseTagFormatter();
        Tags collection = [new Tag("course:777"), new Tag("faculty:it")];

        // Act
        var result = string.Format(mockFormatter, "{0}", collection);

        // Assert
        Assert.Equal("COURSE:777, FACULTY:IT", result);
    }

    sealed class UppercaseTagFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) => arg is Tags tags ? tags.ToString().ToUpperInvariant() : arg?.ToString() ?? string.Empty;
    }

    #endregion

    #region ToString Formatting Tests

    [Fact]
    public void ToString_WhenCollectionIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var collection = new Tags();

        // Act & Assert
        Assert.Empty(collection.ToString());
    }

    [Fact]
    public void ToString_WithDefaultSeparator_ShouldJoinElementsWithStrictComma()
    {
        // Arrange
        Tags collection = [new Tag("faculty:it"), new Tag("course:777")];

        // Act
        var result = collection.ToString();

        // Assert
        Assert.Equal("course:777, faculty:it", result);
    }

    [Fact]
    public void ToString_WithExtremeLengthExceeding256Chars_ShouldFallbackToPoolAndFormatCorrectly()
    {
        // Arrange: 3 tags * 100 chars = 300 chars total length (> 256 threshold)
        var longTag1 = new Tag(new string('x', 100));
        var longTag2 = new Tag(new string('y', 100));
        var longTag3 = new Tag(new string('z', 100));
        Tags collection = [longTag3, longTag1, longTag2];

        // Act
        var result = collection.ToString();

        // Assert
        Assert.Equal(304, result.Length); // 300 elements chars + 4 separator chars (2 intersections * 2 chars)
        Assert.StartsWith(new string('x', 100), result, StringComparison.Ordinal);
        Assert.EndsWith(new string('z', 100), result, StringComparison.Ordinal);
    }

    #endregion
}
