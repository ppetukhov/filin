using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Filin;

/// <summary>
/// Stable hash.
/// </summary>
public static class StableHash
{
    const int MaxStackThreshold = 256;

    internal static readonly uint EmptyHash32 = Hash32([]);

    internal static readonly ulong EmptyHash64 = Hash64([]);

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

        _ = XxHash32.TryHash(data, hash, out _, seed: 0);

        return BinaryPrimitives.ReadInt32LittleEndian(hash); // OK - ORIGIN
    }

    internal static ulong Hash64(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyHash64;
        }

        var requiredSize = Encoding.UTF8.GetMaxByteCount(value.Length);

        byte[]? rentedArray = null;

        try
        {
            Span<byte> buffer = requiredSize <= MaxStackThreshold ? stackalloc byte[requiredSize] : rentedArray = ArrayPool<byte>.Shared.Rent(requiredSize);

            int writtenBytes = Encoding.UTF8.GetBytes(value, buffer);

            return Hash64(buffer[..writtenBytes]);
        }
        finally
        {
            if (rentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }
    }

    internal static uint Hash32(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyHash32;
        }

        var requiredSize = Encoding.UTF8.GetMaxByteCount(value.Length);

        byte[]? rentedArray = null;

        try
        {
            Span<byte> buffer = requiredSize <= MaxStackThreshold ? stackalloc byte[requiredSize] : rentedArray = ArrayPool<byte>.Shared.Rent(requiredSize);

            int writtenBytes = Encoding.UTF8.GetBytes(value, buffer);

            return Hash32(buffer[..writtenBytes]);
        }
        finally
        {
            if (rentedArray is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Hash32(ReadOnlySpan<byte> data) => XxHash32.HashToUInt32(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Hash64(ReadOnlySpan<byte> data) => XxHash64.HashToUInt64(data);
}
