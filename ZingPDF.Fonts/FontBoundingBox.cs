namespace ZingPDF.Fonts;

/// <summary>
/// Bounding box for a font in 1000-unit glyph space.
/// </summary>
public sealed record FontBoundingBox(int Left, int Bottom, int Right, int Top);
