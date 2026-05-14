namespace ZingPDF.Templates;

/// <summary>
/// Describes the severity of a template diagnostic.
/// </summary>
public enum PdfTemplateDiagnosticSeverity
{
    /// <summary>
    /// Informational diagnostic.
    /// </summary>
    Info,

    /// <summary>
    /// Warning diagnostic.
    /// </summary>
    Warning,

    /// <summary>
    /// Error diagnostic that prevents rendering.
    /// </summary>
    Error
}
