using System.Text;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Parsing.Parsers.Objects.LiteralStrings;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Extensions;

public static class LiteralStringExtensions
{
    public static string Decode(this LiteralString literalString, TextDrawingState? textState = null)
    {
        ArgumentNullException.ThrowIfNull(literalString);

        return literalString.Context.Origin switch
        {
            ObjectOrigin.None or
            ObjectOrigin.UserCreated or
            ObjectOrigin.ParsedDocumentObject => DecodeDocumentObjectString([.. literalString.RawBytes]),
            ObjectOrigin.ParsedContentStream when textState is not null => DecodeContentStreamString([.. literalString.RawBytes], textState),
            ObjectOrigin.ParsedContentStream => throw new InvalidOperationException("TextDrawingState must be provided to decode a content stream string."),
            _ => throw new InvalidOperationException($"Unsupported literal string origin: {literalString.Context.Origin}.")
        };
    }

    private static string DecodeDocumentObjectString(byte[] rawBytes)
    {
        if (rawBytes is null || rawBytes.Length == 0)
            return string.Empty;

        // UTF-16BE BOM
        if (rawBytes.Length >= 2 && rawBytes[0] == 0xFE && rawBytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(rawBytes, 2, rawBytes.Length - 2);

        // UTF-8 BOM (PDF 2.0)
        if (rawBytes.Length >= 3 && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF)
            return Encoding.UTF8.GetString(rawBytes, 3, rawBytes.Length - 3);

        // PDFDocEncoding
        var pdfDoc = Encoding.GetEncoding(PDFEncoding.PDFDoc);
        return pdfDoc.GetString(rawBytes);
    }

    private static string DecodeContentStreamString(byte[] rawBytes, TextDrawingState fontState)
    {
        // Here you ask the font to map the raw bytes into a Unicode string.
        return fontState.MapCharacterCode(rawBytes);
    }
}
