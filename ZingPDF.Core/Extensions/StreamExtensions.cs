using System.Globalization;
using System.Text;

namespace ZingPdf.Core.Extensions
{
    internal static class StreamExtensions
    {
        private static readonly Encoding _iso88591 = Encoding.GetEncoding("ISO-8859-1");
        private static readonly byte[] _newLine = _iso88591.GetBytes("\n");

        public static async Task WriteTextAsync(this Stream stream, string text)
        {
            await stream.WriteAsync(_iso88591.GetBytes(text));
        }

        public static async Task WriteIntAsync(this Stream stream, int value)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

        public static async Task WriteLongAsync(this Stream stream, long value)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture));

        public static async Task WriteLongLeftPaddedAsync(this Stream stream, long value, int padToBytes)
            => await stream.WriteTextAsync(value.ToString("G", CultureInfo.InvariantCulture).PadLeft(padToBytes, '0'));

        /// <summary>
        /// Write a single whitespace character to the stream.
        /// </summary>
        public static async Task WriteWhitespaceAsync(this Stream stream) => await stream.WriteTextAsync(Constants.Whitespace);

        /// <summary>
        /// Write a new line character to the stream.
        /// </summary>
        public static async Task WriteNewLineAsync(this Stream stream) => await stream.WriteAsync(_newLine);
    }
}
