namespace ZingPDF.Elements.Drawing.Text.Extraction;

internal readonly record struct TextRun(
    int PageNumber,
    string Text,
    float X,
    float Y,
    float EndX,
    float Height,
    string FontName,
    float FontSize,
    bool AllWhitespace);
