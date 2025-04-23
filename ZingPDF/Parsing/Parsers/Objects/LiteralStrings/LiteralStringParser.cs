using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Syntax.CommonDataStructures.Strings;
using ZingPDF.Syntax.Objects.Strings;

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

            // 4) Strip BOM/preamble bytes and decode to .NET string
            byte[] contentBytes = unescaped.Skip(preambleLen).ToArray();
            string text = encoding.GetString(contentBytes);

            // 5) Map to enum and return
            var enumEnc = MapToLiteralStringEncoding(encoding);
            var result = new LiteralString(text, enumEnc);

            return result;
        }

        private static LiteralStringEncoding MapToLiteralStringEncoding(Encoding enc)
        {
            if (enc is UTF8Encoding) return LiteralStringEncoding.UTF8;
            if (enc is UnicodeEncoding u && u.CodePage == 1201) return LiteralStringEncoding.UTF16BE;
            if (enc is PdfDocEncoding) return LiteralStringEncoding.PDFDocEncoding;
            // default/fallback: treat as PDFDocEncoding
            return LiteralStringEncoding.PDFDocEncoding;
        }
    }
}
