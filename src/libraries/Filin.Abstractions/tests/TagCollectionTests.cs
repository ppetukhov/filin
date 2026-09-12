using System.Globalization;

namespace Filin.Abstractions.Tests;

public class TagCollectionTests
{
    #region Constructor, IsEmpty and Count

    [Fact]
    public void Constructor_EmptyParams_ShouldCreateEmptyCollection()
    {
        // Act
        var collection = new TagCollection();

        // Assert
        collection.IsEmpty.Should().BeTrue();
        collection.Count.Should().Be(0);
        collection.AsSpan().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Constructor_SingleElement_ShouldInitializeCorrectly()
    {
        // Arrange
        var tag = new Tag("course:777");

        // Act
        var collection = new TagCollection(tag);

        // Assert
        collection.IsEmpty.Should().BeFalse();
        collection.Count.Should().Be(1);
        collection[0].Should().Be(tag);
    }

    [Fact]
    public void Constructor_UnsortedElements_ShouldSortAndDeduplicate()
    {
        // Arrange
        var t1 = new Tag("b_tag");
        var t2 = new Tag("a_tag");
        var t3 = new Tag("b_tag"); // Duplicate

        // Act
        var collection = new TagCollection(t1, t2, t3);

        // Assert
        collection.Count.Should().Be(2);
        collection[0].Should().Be(t2); // "a_tag" (sorted)
        collection[1].Should().Be(t1); // "b_tag" (unique)
    }

    #endregion

    #region Indexer and Boundary Errors

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Indexer_InvalidIndexOnEmptyCollection_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new TagCollection();

        // Act
        Action act = () => _ = collection[index];

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-5)]
    public void Indexer_IndexOutOfBounds_ShouldThrowArgumentOutOfRangeException(int index)
    {
        // Arrange
        var collection = new TagCollection(new Tag("t1"), new Tag("t2"));

        // Act
        Action act = () => _ = collection[index];

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_OnEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var collection = new TagCollection();
        var targetTag = new Tag("course:777");

        // Act
        bool result = collection.Contains(targetTag);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_WhenTagExists_ShouldReturnTrue()
    {
        // Arrange
        var t1 = new Tag("a_tag");
        var t2 = new Tag("b_tag");
        var t3 = new Tag("c_tag");
        var collection = new TagCollection(t2, t1, t3);

        // Act & Assert
        collection.Contains(t1).Should().BeTrue();
        collection.Contains(t2).Should().BeTrue();
        collection.Contains(t3).Should().BeTrue();
    }

    #endregion

    #region Add Methods (Merging)

    [Fact]
    public void Add_CollectionWithCollection_ShouldMergeSortAndDeduplicate()
    {
        // Arrange
        var c1 = new TagCollection(new Tag("1_Created"), new Tag("3_Updated"));
        var c2 = new TagCollection(new Tag("3_Updated"), new Tag("2_Deleted"));

        // Act
        TagCollection result = [.. c1, .. c2];

        // Assert
        result.Count.Should().Be(3);
        result[0].Should().Be(new Tag("1_Created"));
        result[1].Should().Be(new Tag("2_Deleted"));
        result[2].Should().Be(new Tag("3_Updated"));
    }

    [Fact]
    public void Add_WithCollectionExpression_ShouldMergeCorrectlyViaCollectionBuilder()
    {
        // Arrange
        var collection = new TagCollection(new Tag("b"));

        // Act
        TagCollection result = [.. collection, new Tag("b"), new Tag("a")];

        // Assert
        result.Count.Should().Be(2);
        result[0].Should().Be(new Tag("a"));
        result[1].Should().Be(new Tag("b"));
    }

    #endregion

    #region Equality and IComparable

    [Fact]
    public void Equals_IdenticalCollections_ShouldReturnTrue()
    {
        // Arrange
        var c1 = new TagCollection(new Tag("a"), new Tag("b"));
        var c2 = new TagCollection(new Tag("b"), new Tag("a"));

        // Act & Assert
        c1.Equals(c2).Should().BeTrue();
        (c1 == c2).Should().BeTrue();
        c1.GetHashCode().Should().Be(c2.GetHashCode());
    }

    [Fact]
    public void CompareTo_DifferentElements_ShouldFollowLexicographicalOrder()
    {
        // Arrange
        var c1 = new TagCollection(new Tag("a"), new Tag("b"));
        var c2 = new TagCollection(new Tag("a"), new Tag("c"));

        // Act & Assert
        c1.CompareTo(c2).Should().BeLessThan(0);
        c2.CompareTo(c1).Should().BeGreaterThan(0);
    }

    #endregion

    #region Enumerator Testing

    [Fact]
    public void Enumerator_ForEachLoop_ShouldIterateInCorrectOrder()
    {
        // Arrange
        var collection = new TagCollection(new Tag("z"), new Tag("y"));
        var iterated = new List<string?>();

        // Act
        foreach (var tag in collection)
        {
            iterated.Add(tag.Value);
        }

        // Assert
        iterated.Should().HaveCount(2);
        iterated.Should().ContainInOrder("y", "z");
    }

    [Fact]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new TagCollection(new Tag("A"));
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
            ex.Message.Should().Be("TagCollection.Enumerator.Current value has not been set.");
        }
    }

    #endregion

    #region Element Search (Contains)

    [Fact]
    public void Contains_WhenTagDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var t1 = new Tag("a_tag");
        var t2 = new Tag("c_tag");
        var collection = new TagCollection(t1, t2);

        var missingInBetween = new Tag("b_tag"); // Sits alphabetically between 'a' and 'c'
        var completelyNewTag = new Tag("z_tag"); // Sits outside the array bounds

        // Act & Assert
        collection.Contains(missingInBetween).Should().BeFalse();
        collection.Contains(completelyNewTag).Should().BeFalse();
    }

    [Fact]
    public void Contains_WithSingleElement_ShouldWorkCorrectly()
    {
        // Arrange
        var tag = new Tag("course:777");
        var collection = new TagCollection(tag);
        var wrongTag = new Tag("course:888");

        // Act & Assert
        collection.Contains(tag).Should().BeTrue();
        collection.Contains(wrongTag).Should().BeFalse();
    }

    #endregion

    #region Relational Operators Tests

    [Fact]
    public void RelationalOperators_WithVariousCollections_ShouldEvaluateCorrectly()
    {
        // Arrange
        TagCollection c1 = [new Tag("a"), new Tag("b")];
        TagCollection c2 = [new Tag("a"), new Tag("c")];
        TagCollection c3 = [new Tag("a"), new Tag("b")];

        // Act & Assert
        (c1 == c3).Should().BeTrue();
        (c1 != c2).Should().BeTrue();
        (c1 < c2).Should().BeTrue("['a', 'b'] precedes ['a', 'c'] lexicographically");
        (c1 <= c3).Should().BeTrue();
        (c2 > c1).Should().BeTrue();
        (c3 >= c1).Should().BeTrue();
    }

    #endregion

    #region ISpanFormattable Tests

    [Fact]
    public void TryFormat_WithSufficientBuffer_ShouldWriteCorrectlyAndReturnTrue()
    {
        // Arrange
        TagCollection collection = [new Tag("course:777"), new Tag("student:123")];
        Span<char> destination = stackalloc char[50];

        // Act
        bool success = collection.TryFormat(destination, out int charsWritten, default, CultureInfo.InvariantCulture);

        // Assert
        success.Should().BeTrue();
        charsWritten.Should().Be(23); // length of "course:777, student:123"
        new string(destination[..charsWritten]).Should().Be("course:777, student:123");
    }

    [Fact]
    public void TryFormat_WithInsufficientBuffer_ShouldReturnFalseAndNotCrash()
    {
        // Arrange
        TagCollection collection = [new Tag("course:777"), new Tag("student:123")];
        Span<char> tinyDestination = stackalloc char[15]; // Not enough space for full collection

        // Act
        bool success = collection.TryFormat(tinyDestination, out int charsWritten, default, CultureInfo.InvariantCulture);

        // Assert
        success.Should().BeFalse("the formatting layout must abort cleanly if the target block span is too small");
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void StringInterpolation_DirectUse_ShouldInvokeSpanFormattableNatively()
    {
        // Arrange
        TagCollection collection = [new Tag("a"), new Tag("b")];

        // Act
        string message = $"Boundaries: {collection}";

        // Assert
        message.Should().Be("Boundaries: a, b", "the string interpolation engine must cleanly parse the custom formattable structure");
    }

    #endregion

    #region ICustomFormatter Integration Tests

    [Fact]
    public void ToString_WithCustomFormatterProvider_ShouldDelegeteToCustomFormatter()
    {
        // Arrange: Создаем кастомный форматтер, который преобразует вывод тегов в верхний регистр
        var mockFormatter = new UppercaseTagFormatter();
        TagCollection collection = [new Tag("course:777"), new Tag("faculty:it")];

        // Act
        string result = string.Format(mockFormatter, "{0}", collection);

        // Assert
        result.Should().Be("COURSE:777, FACULTY:IT", "the structure must respect explicit third-party ICustomFormatter implementations");
    }

    sealed class UppercaseTagFormatter : IFormatProvider, ICustomFormatter
    {
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (arg is TagCollection tags)
            {
                return tags.ToString().ToUpperInvariant();
            }
            return arg?.ToString() ?? string.Empty;
        }
    }

    #endregion

    #region ToString Formatting Tests

    [Fact]
    public void ToString_WhenCollectionIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var collection = new TagCollection();

        // Act & Assert
        collection.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ToString_WithDefaultSeparator_ShouldJoinElementsWithStrictComma()
    {
        // Arrange
        TagCollection collection = [new Tag("faculty:it"), new Tag("course:777")];

        // Act
        string result = collection.ToString();

        // Assert
        result.Should().Be("course:777, faculty:it", "tags must be explicitly separated by a single raw comma character");
    }

    [Fact]
    public void ToString_WithExtremeLengthExceeding256Chars_ShouldFallbackToPoolAndFormatCorrectly()
    {
        // Arrange: 3 tags * 100 chars = 300 chars total length (> 256 threshold)
        var longTag1 = new Tag(new string('x', 100));
        var longTag2 = new Tag(new string('y', 100));
        var longTag3 = new Tag(new string('z', 100));
        TagCollection collection = [longTag3, longTag1, longTag2];

        // Act
        string result = collection.ToString();

        // Assert
        result.Length.Should().Be(304); // 300 elements chars + 4 separator chars (2 intersections * 2 chars)
        result.StartsWith(new string('x', 100)).Should().BeTrue();
        result.EndsWith(new string('z', 100)).Should().BeTrue();
    }

    #endregion
}
