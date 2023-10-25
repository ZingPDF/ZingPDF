using System.Globalization;
using System.Text;

namespace ZingPdf.Core.Extensions
{
    internal static class StreamExtensions
    {
        private static readonly Encoding _defaultEncoding = Encoding.UTF8;

        public static Task WriteTextAsync(this Stream stream, string text)
            => WriteTextAsync(stream, text, _defaultEncoding);

        public static async Task WriteTextAsync(this Stream stream, string text, Encoding encoding)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (text is null) throw new ArgumentNullException(nameof(text));

            await stream.WriteAsync(encoding.GetBytes(text));
        }

        public static async Task WriteCharsAsync(this Stream stream, params char[] characters)
            => await stream.WriteAsync(_defaultEncoding.GetBytes(characters));

        public static async Task WriteIntAsync(this Stream stream, int value)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

        public static async Task WriteLongAsync(this Stream stream, long value)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

        public static async Task WriteLongLeftPaddedAsync(this Stream stream, long value, int padToBytes)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture).PadLeft(padToBytes, '0'));

        /// <summary>
        /// Write a single whitespace character to the stream.
        /// </summary>
        public static async Task WriteWhitespaceAsync(this Stream stream)
            => await stream.WriteCharsAsync(Constants.Whitespace);

        /// <summary>
        /// Write a new line character to the stream.
        /// </summary>
        public static async Task WriteNewLineAsync(this Stream stream)
            => await stream.WriteCharsAsync(Constants.LineFeed);

        /// <summary>
        /// Finds the specified value in the stream and advances its position to it.
        /// </summary>
        public static async Task AdvanceToNextAsync(this Stream stream, char value)
            => await stream.AdvanceAsync(value.ToString(), includeValueInOutput: true);

        /// <summary>
        /// Finds the specified value in the stream and advances its position to it.
        /// </summary>
        public static async Task AdvanceToNextAsync(this Stream stream, string value)
            => await stream.AdvanceAsync(value.ToString(), includeValueInOutput: true);

        /// <summary>
        /// Finds the specified value in the stream and advances its position to it.
        /// </summary>
        public static async Task AdvanceBeyondNextAsync(this Stream stream, char value)
            => await stream.AdvanceAsync(value.ToString(), includeValueInOutput: false);

        /// <summary>
        /// Finds the specified value in the stream and advances its position to it.
        /// </summary>
        public static async Task AdvanceBeyondNextAsync(this Stream stream, string value)
            => await stream.AdvanceAsync(value.ToString(), includeValueInOutput: false);

        /// <summary>
        /// Finds the specified value in the stream and advances its position to it.
        /// </summary>
        private static async Task AdvanceAsync(this Stream stream, string value, bool includeValueInOutput)
        {
            var bufferSize = value.Length;

            byte[] buffer = new byte[bufferSize];
            do
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));

                string content = Encoding.UTF8.GetString(buffer, 0, bufferSize);

                if (content == value)
                {
                    if (includeValueInOutput)
                    {
                        stream.Position -= bufferSize;
                    }

                    break;
                }
                else
                {
                    if (read > 1)
                    {
                        stream.Position -= read - 1;
                    }
                }
            }
            while (stream.Position <= stream.Length - value.Length);
        }

        /// <summary>
        /// Reads the stream into a string until it finds any of the specified characters.
        /// The found character is not included in the output.
        /// </summary>
        public static Task<string> ReadUpToExcludingAsync(this Stream stream, params char[] c)
            => ReadUpToAsync(stream, includeValueInOutput: false, c);

        /// <summary>
        /// Reads the stream into a string until it finds any of the specified characters.
        /// The found character is included in the output.
        /// </summary>
        public static Task<string> ReadUpToIncludingAsync(this Stream stream, params char[] c)
            => ReadUpToAsync(stream, includeValueInOutput: true, c);

        private static async Task<string> ReadUpToAsync(this Stream stream, bool includeValueInOutput, params char[] c)
        {
            var bufferSize = 128;
            var buffer = new byte[bufferSize];

            string content = string.Empty;
            do
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));

                content += Encoding.UTF8.GetString(buffer, 0, read);

                var index = content.IndexOfAny(c);
                if (index != -1)
                {
                    if (includeValueInOutput)
                    {
                        index++;
                    }

                    stream.Position -= read - index;
                    return content[..index];
                }
            }
            while (stream.Position < stream.Length);

            return content;
        }

        /// <summary>
        /// Read stream until the predicate is satisfied.
        /// </summary>
        /// <remarks>
        /// Data is buffered from the stream at the size specified in <paramref name="bufferSize"/>.<para></para>
        /// One character at a time is supplied to your predicate.<para></para>
        /// When this method returns, the stream will be set to the position of the current character.
        /// </remarks>
        public static async Task<string> ReadUntilAsync(this Stream stream, Func<char, bool> condition, int bufferSize = 256)
        {
            var buffer = new byte[bufferSize];
            var content = string.Empty;

            do
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize));
                var str = Encoding.UTF8.GetString(buffer, 0, read);

                content += str;

                for (int i = 0; i < str.Length; i++)
                {
                    char c = str[i];
                    if (condition(c))
                    {
                        stream.Position -= read - i;
                        return content[0..i];
                    }
                }
            }
            while (stream.Position < stream.Length);

            return content;
        }

        /// <summary>
        /// Returns the next <paramref name="numBytes"/> from the stream.
        /// </summary>
        /// <remarks>
        /// This method returns the next specified number of bytes from the stream, decoded as UTF8.<para></para>
        /// If there are fewer than the requested number of bytes left in the stream, the rest of the stream will be returned. <para></para>
        /// The stream will advance by the number of bytes returned.
        /// </remarks>
        public static async Task<string> GetAsync(this Stream stream, int numBytes = 1024)
        {
            var buffer = new byte[numBytes];

            var read = await stream.ReadAsync(buffer.AsMemory(0, numBytes));

            return Encoding.UTF8.GetString(buffer, 0, read);
        }

        /// <summary>
        /// Makes a new <see cref="Stream"/> from the specified range of byte offsets. 
        /// </summary>
        public static async Task<Stream> RangeAsync(this Stream stream, long from, long to)
        {
            if (to > stream.Length) throw new ArgumentOutOfRangeException(nameof(to));

            var originalPosition = stream.Position;

            var bufferSize = 1024;
            var buffer = new byte[bufferSize];

            stream.Position = from;

            var ms = new MemoryStream();

            if (from == to)
            {
                return ms;
            }

            if (from == 0 && to == stream.Length)
            {
                await stream.CopyToAsync(ms);
                stream.Position = originalPosition;
                ms.Position = 0;

                return ms;
            }

            do
            {
                var amountToRead = Math.Min(bufferSize, (int)Math.Min(to - stream.Position, stream.Length));
                var read = await stream.ReadAsync(buffer.AsMemory(0, amountToRead));

                await ms.WriteAsync(buffer.AsMemory(0, read));
            }
            while (stream.Position != to);

            stream.Position = originalPosition;
            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// Advance the stream to the next non-whitespace character.
        /// </summary>
        public static Task AdvancePastWhitepaceAsync(this Stream stream)
        {
            string? str;
            do
            {
                var i = stream.ReadByte();
                str = Encoding.ASCII.GetString(new[] { (byte)i });

                if (!string.IsNullOrWhiteSpace(str))
                {
                    stream.Position--;
                    break;
                }
            }
            while (stream.Position < stream.Length);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Read the remaining contents of the stream.
        /// </summary>
        public static async Task<byte[]> ReadToEndAsync(this Stream stream)
        {
            using var ms = new MemoryStream();

            await stream.CopyToAsync(ms);

            //Array.Copy(stream, stream.Position, )

            return ms.ToArray();
        }
    }
}
