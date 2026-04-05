namespace ZingPDF.GoogleFonts;

/// <summary>
/// Requests a specific Google Font family and variant.
/// </summary>
public sealed record GoogleFontRequest
{
    public required string Family { get; init; }
    public string Variant { get; init; } = "regular";
    public bool PreferVariableFont { get; init; }
}
