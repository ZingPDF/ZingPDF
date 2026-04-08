namespace ZingPDF;

/// <summary>
/// Describes the result of applying pending redaction marks.
/// </summary>
public sealed class PdfRedactionReport
{
    /// <summary>
    /// The number of redaction marks applied.
    /// </summary>
    public required int AppliedMarkCount { get; init; }

    /// <summary>
    /// The 1-based page numbers touched by the redaction operation.
    /// </summary>
    public required IReadOnlyList<int> PagesTouched { get; init; }

    /// <summary>
    /// Warnings that callers should surface or log alongside the redaction result.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
