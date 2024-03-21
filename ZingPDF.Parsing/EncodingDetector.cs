using System.Text;

namespace ZingPDF.Parsing
{
    internal class EncodingResult
    {
        public EncodingResult(Encoding stringEncoding, bool isOctal = false)
        {
            StringEncoding = stringEncoding ?? throw new ArgumentNullException(nameof(stringEncoding));
            IsOctal = isOctal;
        }

        /// <summary>
        /// The encoding used for the string.
        /// </summary>
        /// <remarks>
        /// Note: this may not be the encoding used to encode the bytes into the file.
        /// Strings may also use octal codes to represent non-ASCII characters.
        /// In this case, the <see cref="IsOctal"/> property will be true, 
        /// and the string will be written to the file using ASCII encoding.
        /// </remarks>
        public Encoding StringEncoding { get; }

        /// <summary>
        /// True if the string has been converted to represent non-ASCII characters as octals.
        /// </summary>
        public bool IsOctal { get; }
    }

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
        public async Task<EncodingResult> DetectAsync(Stream stream, Encoding? defaultEncoding = null, bool advanceStreamBeyondByteOrderMark = true)
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
                return new(defaultEncoding);
            }

            if (buffer[0] == _utf16be[0] && buffer[1] == _utf16be[1])
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += 2;
                }

                return new(Encoding.BigEndianUnicode);
            }

            if (read < 3)
            {
                return new(defaultEncoding);
            }

            if (buffer[0] == _utf8[0] && buffer[1] == _utf8[1] && buffer[2] == _utf8[2])
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += 3;
                }

                return new(Encoding.UTF8);
            }

            if (read < 8)
            {
                return new(defaultEncoding);
            }

            var content = Encoding.ASCII.GetString(buffer);

            if (content.StartsWith(_utf16beOctal))
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += _utf16beOctal.Length;
                }

                return new(Encoding.BigEndianUnicode, isOctal: true);
            }

            if (read < 12)
            {
                return new(defaultEncoding);
            }

            if (content.StartsWith(_utf8Octal))
            {
                if (advanceStreamBeyondByteOrderMark)
                {
                    stream.Position += _utf8Octal.Length;
                }

                return new(Encoding.UTF8, isOctal: true);
            }

            return new(defaultEncoding);
        }
    }
}
