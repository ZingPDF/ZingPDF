namespace ZingPDF;

/// <summary>
/// Describes how a redaction mark was created.
/// </summary>
public enum PdfRedactionKind
{
    /// <summary>
    /// The mark was created from an exact text match.
    /// </summary>
    TextMatch,

    /// <summary>
    /// The mark was created from a regular-expression text match.
    /// </summary>
    RegexMatch,

    /// <summary>
    /// The mark was created from an explicit page region.
    /// </summary>
    Region
}
