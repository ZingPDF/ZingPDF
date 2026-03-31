namespace ZingPDF.Fonts;

/// <summary>
/// Parsed TrueType font data and metrics suitable for registration in a PDF document.
/// </summary>
public sealed class TrueTypeFontFace
{
    public required string FontName { get; init; }
    public required string FamilyName { get; init; }
    public required byte[] FontData { get; init; }
    public required FontMetrics Metrics { get; init; }
    public required FontBoundingBox BoundingBox { get; init; }
    public required TrueTypeEmbeddingPermissions EmbeddingPermissions { get; init; }
    public required IReadOnlyDictionary<byte, int> WidthsByCharacterCode { get; init; }
    public required int MissingWidth { get; init; }
    public required int AverageWidth { get; init; }
    public required int MaxWidth { get; init; }
}
