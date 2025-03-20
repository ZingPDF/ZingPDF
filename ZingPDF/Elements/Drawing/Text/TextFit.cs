using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Drawing.Text;

public record TextFit
{
    public required Number FontSize { get; init; }
    public required Coordinate TextOrigin { get; init; }
}
