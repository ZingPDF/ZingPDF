using System.Text;

namespace ZingPdf.Core.Parsing
{
    internal class EncodingDetector
    {
        private readonly Encoding _defaultEncoding = Encoding.Latin1;

        // hexadecimal
        private readonly byte[] _utf8 = [0xEF, 0xBB, 0xBF];
        private readonly byte[] _utf16be = [0xFE, 0xFF];

        // octal
        private readonly string _utf8Octal = "\\357\\273\\277";
        private readonly string _utf16beOctal = "\\376\\377";

        /// <summary>
        /// Returns an encoding based on any byte order marks present at the current position in the stream.
        /// </summary>
        /// <remarks>
        /// This class supports the detection of byte order marks for UTF8 and UTF16BE. The BOM can be specified in its
        /// natural form, e.g. as bytes, or as an ASCII string containing an octal representation of the values e.g. "\357\273\277".
        /// </remarks>
        /// <param name="stream">The stream from which to detect the encoding.</param>
        /// <param name="defaultEncoding">The encoding to return if a valid encoding cannot be detected.</param>
        public async Task<Encoding> DetectAsync(Stream stream, Encoding? defaultEncoding = null, bool advanceStreamBeyondByteOrderMark = true)
        {
            defaultEncoding ??= _defaultEncoding;

            // The longest preamble is 12 bytes, read up to 12 bytes
            var buffer = new byte[12];
            var read = await stream.ReadAsync(buffer);

            if (read > 0)
            {
                stream.Position -= read;
            }

            if (read < 2)
            {
                return defaultEncoding;
            }

            if (buffer[0] == _utf16be[0] && buffer[1] == _utf16be[1])
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += 2;
                }

                return Encoding.BigEndianUnicode;
            }

            if (read < 3)
            {
                return defaultEncoding;
            }

            if (buffer[0] == _utf8[0] && buffer[1] == _utf8[1] && buffer[2] == _utf8[2])
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += 3;
                }

                return Encoding.UTF8;
            }

            if (read < 8)
            {
                return defaultEncoding;
            }

            var content = Encoding.ASCII.GetString(buffer);

            if (content.StartsWith(_utf16beOctal))
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += _utf16beOctal.Length;
                }

                return Encoding.BigEndianUnicode;
            }

            if (read < 12)
            {
                return defaultEncoding;
            }

            if (content.StartsWith(_utf8Octal))
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += _utf8Octal.Length;
                }

                return Encoding.UTF8;
            }

            return defaultEncoding;
        }
    }
}
