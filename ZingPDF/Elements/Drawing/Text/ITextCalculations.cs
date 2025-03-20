using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Drawing.Text;

internal interface ITextCalculations
{
    TextFit CalculateTextFit(string fontName, Rectangle boundingBox, string text);
}
