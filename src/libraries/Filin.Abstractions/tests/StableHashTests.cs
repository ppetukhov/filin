namespace Filin.Abstractions.Tests;

public sealed class StableHashTests
{
    internal const ulong HASH64_OF_EMPTY = 17241709254077376921UL;

    public static readonly TheoryData<string, ulong> TextWithExpectedHash = new()
    {
        { "Lorem Ipsum is simply dummy text of the printing and typesetting industry", 12843400592381000807UL },
        { "Contrary to popular belief, Lorem Ipsum is not simply random text.", 6378906660793192535UL },
        { @"!""#$%&'()*+,-./0123456789:;<=>?@[\]^_`~", 13634593389276835636UL },
    };

    public static readonly TheoryData<string?, ulong> NullOrEmptyStringWithExpectedHash = new()
    {
        { "", HASH64_OF_EMPTY },
        { null, HASH64_OF_EMPTY }
    };

    [Theory]
    [MemberData(nameof(TextWithExpectedHash))]
    [MemberData(nameof(NullOrEmptyStringWithExpectedHash))]
    public void StableHash_ComputeHash_From_Bytes(string? value, ulong expectedHash)
    {
        var result = StableHash.Hash64(value);

        result.Should().Be(expectedHash);
    }
}
