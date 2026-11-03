namespace ZingPDF.Text;

/// <summary>Controls baseline placement of explicit lines of text in page coordinates.</summary>
public sealed record TextPlacementOptions
{
    /// <summary>Counterclockwise rotation around the first line's baseline origin, in degrees.</summary>
    public double RotationDegrees { get; init; }
    /// <summary>Distance between baselines in local text units. Null uses 1.25 times the font size.</summary>
    public double? LineHeight { get; init; }
}
