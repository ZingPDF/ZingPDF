namespace ZingPDF.Templates;

/// <summary>
/// Describes a message produced while loading, parsing, rendering, or converting a PDF template.
/// </summary>
public sealed record PdfTemplateDiagnostic(
    PdfTemplateDiagnosticSeverity Severity,
    string Message,
    string? SourceName = null,
    int? Line = null,
    int? Column = null);
