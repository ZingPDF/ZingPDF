using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Parsing.Parsers.Objects.LiteralStrings;

/// <summary>
/// Parses a PDF literal string from a content stream, handling nested parentheses,
/// escape sequences, octal codes, line continuations, and various text encodings.
/// </summary>
internal class LiteralStringParser : IParser<LiteralString>
{
    public async ITask<LiteralString> ParseAsync(Stream stream, ObjectContext context)
    {
        await stream.AdvanceToNextAsync(Constants.Characters.LeftParenthesis);

        // 1) Read raw bytes of the literal (excluding the outer parentheses)
        byte[] raw = PdfLiteralStringReader.ReadRawLiteral(stream);

        // TODO: this won't work for encrypted strings, consider moving this to the decode extension method?
        // 2) Unescape octal sequences and backslash escapes to get actual bytes
        byte[] unescaped = PdfStringUnescaper.Unescape(raw);

        // 3) Return
        var result = LiteralString.FromBytes(unescaped, context);

        return result;
    }
}
