using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Extensions;

public static class PdfStringExtensions
{
    public static string Decode(this PdfString pdfString, TextDrawingState? textState = null)
    {
        ArgumentNullException.ThrowIfNull(pdfString);

        return pdfString.Context.Origin switch
        {
            ObjectOrigin.None or
            ObjectOrigin.UserCreated or
            ObjectOrigin.ImplicitOperatorConversion or
            ObjectOrigin.ParsedDocumentObject => DecodeDocumentObjectString(pdfString),
            ObjectOrigin.ParsedContentStream when textState is not null => DecodeContentStreamString(pdfString, textState),
            ObjectOrigin.ParsedContentStream => throw new InvalidOperationException("TextDrawingState must be provided to decode a content stream string."),
            _ => throw new InvalidOperationException($"Unsupported string origin: {pdfString.Context.Origin}.")
        };
    }

    private static string DecodeDocumentObjectString(PdfString pdfString)
    {
        return pdfString.AsText().DecodeText();
    }

    private static string DecodeContentStreamString(PdfString pdfString, TextDrawingState fontState)
    {
        // Here you ask the font to map the raw bytes into a Unicode string.
        return fontState.MapCharacterCode(pdfString.Bytes);
    }
}
