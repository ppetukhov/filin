using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Filin;

/// <summary>
/// Stable hash.
/// </summary>
public static class StableHash
{
    /// <summary>
    /// Computes a hash digest of the input.
    /// </summary>
    /// <param name="data">
    /// The input data.
    /// </param>
    /// <returns>
    /// A hash digest of the input.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeHash(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[sizeof(uint)];

        XxHash32.TryHash(data, hash, out _, seed: 0);

        return BinaryPrimitives.ReadInt32LittleEndian(hash); // BinaryPrimitives.ReadUInt32LittleEndian(hash);
    }
}
