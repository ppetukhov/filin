using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Filin.EventStore.Postgres;

sealed partial class EventStore
{
    readonly struct CommandTextCacheKey : IEquatable<CommandTextCacheKey>
    {
        const int MAX_SIZE = 16;

        [InlineArray(MAX_SIZE)]
        struct Buffer
        {
            byte _element0;
        }

        readonly Buffer _buffer;

        byte EventsCount => _buffer[0];
        byte FailIfMatchQueryByteCount => _buffer[1];
        int TotalBytes => 2 + EventsCount + FailIfMatchQueryByteCount;

        CommandTextCacheKey(ReadOnlySpan<byte> payload)
        {
            payload.CopyTo(_buffer);
        }

        internal static ReadOnlySpan<byte> EventTagsCount(ref CommandTextCacheKey item) => item._buffer[2..][..item.EventsCount];
        internal static ReadOnlySpan<(byte EventTypesCount, byte TagsCount)> FailIfMatch(ref CommandTextCacheKey item) =>
            MemoryMarshal.Cast<byte, (byte, byte)>(item._buffer[(2 + item.EventsCount)..][..item.FailIfMatchQueryByteCount]);

        internal static bool TryCreate(ICollection<DcbEvent> dcbEvents, DcbQuery failIfMatchQuery, out CommandTextCacheKey queryKey)
        {
            var bytesCount = 2 + dcbEvents.Count + (2 * failIfMatchQuery.Count);

            if (bytesCount > MAX_SIZE)
            {
                queryKey = default;
                return false;
            }

            Span<byte> totalBytes = stackalloc byte[bytesCount];

            var i = 0;

            totalBytes[i++] = (byte)dcbEvents.Count;
            totalBytes[i++] = (byte)(2 * failIfMatchQuery.Count);

            foreach (var item in dcbEvents)
            {
                totalBytes[i++] = (byte)item.Tags.Count;
            }

            foreach (var item in failIfMatchQuery)
            {
                totalBytes[i++] = (byte)item.EventTypes.Count;
                totalBytes[i++] = (byte)item.Tags.Count;
            }

            queryKey = new CommandTextCacheKey(totalBytes);

            return true;
        }

        internal static bool TryCreate(DcbEvent dcbEvent, DcbQuery failIfMatchQuery, out CommandTextCacheKey queryKey)
        {
            var bytesCount = 2 + 1 + (2 * failIfMatchQuery.Count);

            if (bytesCount > MAX_SIZE)
            {
                queryKey = default;
                return false;
            }

            Span<byte> totalBytes = stackalloc byte[bytesCount];

            var i = 0;

            totalBytes[i++] = 1;
            totalBytes[i++] = (byte)(2 * failIfMatchQuery.Count);

            totalBytes[i++] = (byte)dcbEvent.Tags.Count;

            foreach (var item in failIfMatchQuery)
            {
                totalBytes[i++] = (byte)item.EventTypes.Count;
                totalBytes[i++] = (byte)item.Tags.Count;
            }

            queryKey = new CommandTextCacheKey(totalBytes);

            return true;
        }

        internal static bool TryCreate(DcbQuery query, out CommandTextCacheKey queryKey)
        {
            var bytesCount = 2 + 0 + (2 * query.Count);

            if (bytesCount > MAX_SIZE)
            {
                queryKey = default;
                return false;
            }

            Span<byte> totalBytes = stackalloc byte[bytesCount];

            var i = 0;

            totalBytes[i++] = 0;
            totalBytes[i++] = (byte)(2 * query.Count);

            foreach (var item in query)
            {
                totalBytes[i++] = (byte)item.EventTypes.Count;
                totalBytes[i++] = (byte)item.Tags.Count;
            }

            queryKey = new CommandTextCacheKey(totalBytes);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            ReadOnlySpan<byte> span = _buffer;

            var hash = new HashCode();
            hash.Add(EventsCount);
            hash.Add(FailIfMatchQueryByteCount);
            hash.AddBytes(span[..TotalBytes]);

            return hash.ToHashCode();
        }

        public bool Equals(CommandTextCacheKey other)
        {
            if (EventsCount != other.EventsCount || FailIfMatchQueryByteCount != other.FailIfMatchQueryByteCount) return false;

            ReadOnlySpan<byte> thisSpan = _buffer;
            ReadOnlySpan<byte> otherSpan = other._buffer;

            return thisSpan[..TotalBytes].SequenceEqual(otherSpan[..TotalBytes]);
        }

        public override bool Equals(object? obj) => obj is CommandTextCacheKey other && Equals(other);

        public static bool operator ==(CommandTextCacheKey left, CommandTextCacheKey right) => left.Equals(right);
        public static bool operator !=(CommandTextCacheKey left, CommandTextCacheKey right) => !left.Equals(right);


    }
}
