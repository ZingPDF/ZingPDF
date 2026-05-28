using ZingPDF.Elements.Drawing;
using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Rendering;

/// <summary>
/// Describes the visible display geometry of a PDF page.
/// </summary>
/// <remarks>
/// Page coordinates use the PDF bottom-left origin. Display coordinates use a top-left
/// origin after the page crop and clockwise page rotation have been applied. Values are
/// expressed in PDF default user-space units, which are normally points (1/72 inch).
/// </remarks>
public sealed record PdfPageGeometry
{
    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the resolved media box in unrotated page coordinates.
    /// </summary>
    public required Rectangle MediaBox { get; init; }

    /// <summary>
    /// Gets the resolved crop box in unrotated page coordinates.
    /// </summary>
    /// <remarks>
    /// When no crop box is defined, this value is equal to <see cref="MediaBox"/>.
    /// </remarks>
    public required Rectangle CropBox { get; init; }

    /// <summary>
    /// Gets the displayed page bounds in unrotated page coordinates.
    /// </summary>
    /// <remarks>
    /// This is the intersection of <see cref="MediaBox"/> and <see cref="CropBox"/>.
    /// </remarks>
    public required Rectangle VisibleBox { get; init; }

    /// <summary>
    /// Gets the clockwise page rotation, normalised to 0, 90, 180, or 270 degrees.
    /// </summary>
    public required int RotationDegrees { get; init; }

    /// <summary>
    /// Gets the displayed width after cropping and page rotation have been applied.
    /// </summary>
    public required double DisplayWidth { get; init; }

    /// <summary>
    /// Gets the displayed height after cropping and page rotation have been applied.
    /// </summary>
    public required double DisplayHeight { get; init; }

    /// <summary>
    /// Converts a point in PDF page coordinates to displayed-page coordinates.
    /// </summary>
    /// <param name="pagePoint">A point using the PDF bottom-left origin.</param>
    /// <returns>A point using the displayed page's top-left origin.</returns>
    public Coordinate PageToDisplay(Coordinate pagePoint)
    {
        ArgumentNullException.ThrowIfNull(pagePoint);

        var x = pagePoint.X - VisibleBox.LowerLeft.X;
        var y = pagePoint.Y - VisibleBox.LowerLeft.Y;
        var width = (double)VisibleBox.Width;
        var height = (double)VisibleBox.Height;

        return RotationDegrees switch
        {
            0 => new Coordinate(x, height - y),
            90 => new Coordinate(y, x),
            180 => new Coordinate(width - x, y),
            270 => new Coordinate(height - y, width - x),
            _ => throw new InvalidOperationException("Page rotation must be normalised before mapping coordinates.")
        };
    }

    /// <summary>
    /// Converts a displayed-page point to PDF page coordinates.
    /// </summary>
    /// <param name="displayPoint">A point using the displayed page's top-left origin.</param>
    /// <returns>A point using the PDF bottom-left origin.</returns>
    public Coordinate DisplayToPage(Coordinate displayPoint)
    {
        ArgumentNullException.ThrowIfNull(displayPoint);

        var width = (double)VisibleBox.Width;
        var height = (double)VisibleBox.Height;
        var relativePagePoint = RotationDegrees switch
        {
            0 => new Coordinate(displayPoint.X, height - displayPoint.Y),
            90 => new Coordinate(displayPoint.Y, displayPoint.X),
            180 => new Coordinate(width - displayPoint.X, displayPoint.Y),
            270 => new Coordinate(width - displayPoint.Y, height - displayPoint.X),
            _ => throw new InvalidOperationException("Page rotation must be normalised before mapping coordinates.")
        };

        return new Coordinate(
            relativePagePoint.X + VisibleBox.LowerLeft.X,
            relativePagePoint.Y + VisibleBox.LowerLeft.Y);
    }
}
