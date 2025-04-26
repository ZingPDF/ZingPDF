using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding.PDFDocEncoding;

namespace ZingPDF.Parsing.Parsers.Objects.LiteralStrings
{
    /// <summary>
    /// Parses a PDF literal string from a content stream, handling nested parentheses,
    /// escape sequences, octal codes, line continuations, and various text encodings.
    /// </summary>
    internal class LiteralStringParser : IObjectParser<LiteralString>
    {
        public async ITask<LiteralString> ParseAsync(Stream stream)
        {
            await stream.AdvanceToNextAsync(Constants.Characters.LeftParenthesis);

            // 1) Read raw bytes of the literal (excluding the outer parentheses)
            byte[] raw = PdfLiteralStringReader.ReadRawLiteral(stream);

            // 2) Unescape octal sequences and backslash escapes to get actual bytes
            byte[] unescaped = PdfStringUnescaper.Unescape(raw);

            // 3) Detect encoding and BOM length from unescaped bytes
            var detection = PdfStringEncodingDetector.Detect(unescaped);
            Encoding encoding = detection.Encoding;
            int preambleLen = detection.PreambleLength;

            // 4) Strip BOM/preamble bytes
            byte[] contentBytes = [.. unescaped.Skip(preambleLen)];

            // 5) Return
            var result = new LiteralString(contentBytes, MapToLiteralStringEncoding(encoding));

            return result;
        }

        private static LiteralStringEncoding MapToLiteralStringEncoding(Encoding enc)
        {
            if (enc is UTF8Encoding) return LiteralStringEncoding.UTF8;
            if (enc is UnicodeEncoding u && u.CodePage == 1201) return LiteralStringEncoding.UTF16BE;
            if (enc is PDFDocEncoding) return LiteralStringEncoding.PDFDocEncoding;
            // default/fallback: treat as PDFDocEncoding
            return LiteralStringEncoding.PDFDocEncoding;
        }
    }
}
