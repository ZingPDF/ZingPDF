using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Text.Editing;

/// <summary>A single baseline of existing PDF text, identified in its current content-stream revision.</summary>
public sealed record PdfEditableTextLine
{
    /// <summary>An opaque source identifier. Obtain a fresh line after editing page content.</summary>
    public required string Id { get; init; }
    /// <summary>The 1-based source page number.</summary>
    public required int PageNumber { get; init; }
    /// <summary>The decoded Unicode text, including inferred word spaces between adjacent fragments.</summary>
    public required string Text { get; init; }
    /// <summary>The original PDF font or font-family name.</summary>
    public required string FontName { get; init; }
    /// <summary>The font size in the text object's local coordinate system.</summary>
    public required double FontSize { get; init; }
    /// <summary>The page-space text-matrix origin, before text rise, page crop and page rotation.</summary>
    public required ZingPDF.Elements.Drawing.Coordinate BaselineOrigin { get; init; }
    /// <summary>The axis-aligned page-space bounds, before page crop and rotation are applied.</summary>
    public required Rectangle Bounds { get; init; }
    /// <summary>Whether this line can be edited without changing unsupported text painting or layout.</summary>
    public bool CanEdit { get; init; }
    /// <summary>A concrete reason when the line cannot be edited.</summary>
    public string? UnsupportedReason { get; init; }
    /// <summary>Whether the original font program is embedded in the document.</summary>
    public bool IsFontEmbedded { get; init; }
    /// <summary>Whether the original font's name contains a PDF subset tag.</summary>
    public bool IsFontSubset { get; init; }
}

/// <summary>Controls replacement of one existing line. New fonts are embedded automatically.</summary>
public sealed record PdfTextReplacementOptions
{
    /// <summary>An optional font registered with this PDF. Cannot be combined with TrueTypeFontData.</summary>
    public PdfFont? Font { get; init; }
    /// <summary>
    /// Optional complete TrueType font bytes. Empty retains the original font and encoding.
    /// The supplied program must permit PDF embedding and cover every replacement character.
    /// </summary>
    public ReadOnlyMemory<byte> TrueTypeFontData { get; init; }
    /// <summary>An optional name for the replacement font. Does not affect glyph coverage.</summary>
    public string? FontName { get; init; }
    /// <summary>An optional replacement size in local text units. Null preserves the original size.</summary>
    public double? FontSize { get; init; }
    /// <summary>Whether to permit the replacement to extend beyond its available horizontal space.</summary>
    public bool AllowOverflow { get; init; }
}

/// <summary>The result of checking replacement text before changing a content stream.</summary>
public sealed record PdfTextEditCheck
{
    /// <summary>Whether the requested replacement can be applied with the supplied options.</summary>
    public required bool CanApply { get; init; }
    /// <summary>Whether another font is necessary to encode the replacement text.</summary>
    public bool RequiresFontChange { get; init; }
    /// <summary>Whether a supplied or already embedded TrueType program will be embedded/re-mapped automatically.</summary>
    public bool WillEmbedFont { get; init; }
    /// <summary>The missing Unicode characters, without duplicates.</summary>
    public string MissingCharacters { get; init; } = "";
    /// <summary>Whether replacement text would exceed the available line width.</summary>
    public bool WouldOverflow { get; init; }
    /// <summary>The replacement's baseline advance, in local text units.</summary>
    public double ReplacementWidth { get; init; }
    /// <summary>The available width to the next text line fragment or visible page edge, in local text units.</summary>
    public double AvailableWidth { get; init; }
    /// <summary>A user-readable explanation of a rejected replacement.</summary>
    public string? Message { get; init; }
}
