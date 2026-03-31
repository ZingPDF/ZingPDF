namespace ZingPDF.GoogleFonts;

/// <summary>
/// Metadata returned for a Google Font family.
/// </summary>
public sealed class GoogleFontFamily
{
    public required string Family { get; init; }
    public string? Category { get; init; }
    public required IReadOnlyDictionary<string, Uri> Variants { get; init; }
}
