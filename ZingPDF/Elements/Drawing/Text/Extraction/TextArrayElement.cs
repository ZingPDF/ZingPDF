using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Drawing.Text.Extraction;

internal readonly record struct TextArrayElement
{
    private TextArrayElement(byte[] textBytes, PdfStringSyntax syntax)
    {
        IsText = true;
        TextBytes = textBytes;
        StringSyntax = syntax;
        Adjustment = 0;
    }

    private TextArrayElement(double adjustment)
    {
        IsText = false;
        TextBytes = null;
        StringSyntax = default;
        Adjustment = adjustment;
    }

    public bool IsText { get; }
    public byte[]? TextBytes { get; }
    public PdfStringSyntax StringSyntax { get; }
    public double Adjustment { get; }

    public static TextArrayElement ForText(byte[] textBytes, PdfStringSyntax syntax) => new(textBytes, syntax);
    public static TextArrayElement ForAdjustment(double adjustment) => new(adjustment);
}
