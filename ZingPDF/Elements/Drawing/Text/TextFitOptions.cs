namespace ZingPDF.Elements.Drawing.Text;

internal sealed record TextFitOptions
{
    public double? RequestedFontSize { get; init; }
    public int Quadding { get; init; }
    public bool IsMultiline { get; init; }
    public bool IsComb { get; init; }
    public bool DoNotScroll { get; init; }
    public int? MaxLength { get; init; }
}
