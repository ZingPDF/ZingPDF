using System.Text;
using ZingPDF.Parsing.Parsers;

namespace ZingPDF.Parsing
{
    internal class ObjectFinder
    {
        private const int BufferSize = 1024;

        public async Task<long?> FindAsync(Stream stream, string token, bool forwards = true, int limit = 1024)
        {
            if (!stream.CanSeek)
            {
                throw new ParserException();
            }

            byte[] tokenBytes = Encoding.ASCII.GetBytes(token);
            if (tokenBytes.Length == 0)
            {
                return stream.Position;
            }

            return forwards
                ? await FindForwardsAsync(stream, tokenBytes, limit)
                : await FindBackwardsAsync(stream, tokenBytes, limit);
        }

        private static async Task<long?> FindBackwardsAsync(Stream stream, byte[] tokenBytes, int limit)
        {
            long originalPosition = stream.Position;
            long windowStart = Math.Max(0, stream.Length - limit);
            int windowLength = (int)(stream.Length - windowStart);
            byte[] buffer = new byte[windowLength];

            stream.Position = windowStart;
            int read = await stream.ReadAsync(buffer.AsMemory(0, windowLength));
            int matchIndex = LastIndexOf(buffer.AsSpan(0, read), tokenBytes);

            if (matchIndex < 0)
            {
                stream.Position = originalPosition;
                return null;
            }

            stream.Position = windowStart + matchIndex;
            return stream.Position;
        }

        private static async Task<long?> FindForwardsAsync(Stream stream, byte[] tokenBytes, int limit)
        {
            long originalPosition = stream.Position;
            long searchStart = stream.Position;
            long searchEnd = Math.Min(stream.Length, searchStart + limit);
            byte[] buffer = new byte[BufferSize + tokenBytes.Length - 1];
            int overlapLength = 0;

            while (stream.Position < searchEnd)
            {
                int readSize = (int)Math.Min(BufferSize, searchEnd - stream.Position);
                int read = await stream.ReadAsync(buffer.AsMemory(overlapLength, readSize));
                if (read == 0)
                {
                    break;
                }

                int totalLength = overlapLength + read;
                int matchIndex = IndexOf(buffer.AsSpan(0, totalLength), tokenBytes);
                if (matchIndex >= 0)
                {
                    stream.Position = (stream.Position - read) - overlapLength + matchIndex;
                    return stream.Position;
                }

                overlapLength = Math.Min(tokenBytes.Length - 1, totalLength);
                if (overlapLength > 0)
                {
                    buffer.AsSpan(totalLength - overlapLength, overlapLength)
                        .CopyTo(buffer);
                }
            }

            stream.Position = originalPosition;
            return null;
        }

        private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
        {
            if (needle.Length > haystack.Length)
            {
                return -1;
            }

            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int LastIndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
        {
            if (needle.Length > haystack.Length)
            {
                return -1;
            }

            for (int i = haystack.Length - needle.Length; i >= 0; i--)
            {
                if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
