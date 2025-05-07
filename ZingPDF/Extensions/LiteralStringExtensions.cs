using System.Text;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text.Encoding;

namespace ZingPDF.Extensions;

public static class LiteralStringExtensions
{
    public static string Decode(this LiteralString literalString, TextDrawingState? textState = null)
    {
        ArgumentNullException.ThrowIfNull(literalString);

        return literalString.Origin switch
        {
            ObjectOrigin.UserCreated => Encoding.ASCII.GetString([.. literalString.RawBytes]),
            ObjectOrigin.ParsedDocumentObject => DecodeDocumentObjectString([.. literalString.RawBytes]),
            ObjectOrigin.ParsedContentStream when textState is not null => DecodeContentStreamString([.. literalString.RawBytes], textState),
            ObjectOrigin.ParsedContentStream => throw new InvalidOperationException("TextDrawingState must be provided to decode a content stream string."),
            _ => throw new InvalidOperationException($"Unsupported literal string origin: {literalString.Origin}.")
        };
    }

    private static string DecodeDocumentObjectString(byte[] rawBytes)
    {
        // Assume document strings follow the standard:
        // - If starts with BOM (0xFE 0xFF), it's UTF-16BE.
        // - Else try PDFDocEncoding first, fallback to UTF-8 if needed.

        if (rawBytes.Length >= 2 && rawBytes[0] == 0xFE && rawBytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(rawBytes.Skip(2).ToArray());
        }

        try
        {
            var pdfDocEncoding = Encoding.GetEncoding(PDFEncoding.PDFDoc);
            return pdfDocEncoding.GetString(rawBytes);
        }
        catch
        {
            // fallback if something fails
            return Encoding.UTF8.GetString(rawBytes);
        }
    }

    private static string DecodeContentStreamString(byte[] rawBytes, TextDrawingState fontState)
    {
        // Here you ask the font to map the raw bytes into a Unicode string.
        return fontState.MapCharacterCode(rawBytes);
    }
}
