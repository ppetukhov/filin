using System.Text;

namespace Filin.Abstractions.Tests;

public sealed class StableHashTests
{
    internal const int HASHCODE_OF_EMPTY = 90033154;

    public static readonly TheoryData<byte[], int> ByteArrayWithExpectedHash = new()
    {
        { Encoding.UTF8.GetBytes("Lorem Ipsum is simply dummy text of the printing and typesetting industry"), 2087748517 },
        { Encoding.UTF8.GetBytes("Contrary to popular belief, Lorem Ipsum is not simply random text."), 332455997 },
        { Encoding.UTF8.GetBytes(@"!""#$%&'()*+,-./0123456789:;<=>?@[\]^_`~"), -1068000349 },
        { [0], 1051747791 },
        { [0, 0], -177558859 },
        { [0, 0, 0, 0], 1775883784 },
        { [0, 0, 0, 0, 0, 0, 0, 0], 328578014 },
    };

    public static readonly TheoryData<byte[]?, int> NullOrEmptyByteArraysWithExpectedHash = new()
    {
        { [], HASHCODE_OF_EMPTY },
        { null, HASHCODE_OF_EMPTY }
    };

    [Theory]
    [MemberData(nameof(ByteArrayWithExpectedHash))]
    [MemberData(nameof(NullOrEmptyByteArraysWithExpectedHash))]
    public void StableHash_ComputeHash_From_Bytes(byte[]? bytes, int expectedHash)
    {
        var result = StableHash.ComputeHash(bytes);

        result.Should().Be(expectedHash);
    }
}
