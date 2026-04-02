namespace ZingPDF.Elements.Drawing.Text.Extraction;

public sealed class TextExtractionResult
{
    public required TextExtractionOutputKind OutputKind { get; init; }
    public string? PlainText { get; init; }
    public IReadOnlyList<ExtractedText>? Segments { get; init; }
    public IReadOnlyList<GlyphRun>? Letters { get; init; }
}
