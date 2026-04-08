using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF;

/// <summary>
/// Represents a pending redaction mark on a page.
/// </summary>
public sealed record PdfRedactionMark
{
    /// <summary>
    /// The 1-based page number containing the redaction mark.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// The bounds to redact in page coordinates.
    /// </summary>
    public required Rectangle Bounds { get; init; }

    /// <summary>
    /// How the mark was created.
    /// </summary>
    public required PdfRedactionKind Kind { get; init; }

    /// <summary>
    /// The matched text when the mark came from text extraction.
    /// </summary>
    public string? SourceText { get; init; }
}
