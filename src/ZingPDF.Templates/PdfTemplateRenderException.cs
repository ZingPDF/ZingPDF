namespace ZingPDF.Templates;

/// <summary>
/// Represents an error that occurs while rendering a PDF template.
/// </summary>
public sealed class PdfTemplateRenderException : Exception
{
    /// <summary>
    /// Creates a template rendering exception.
    /// </summary>
    public PdfTemplateRenderException(string message)
        : this(message, [], null)
    {
    }

    /// <summary>
    /// Creates a template rendering exception with an inner exception.
    /// </summary>
    public PdfTemplateRenderException(string message, Exception innerException)
        : this(message, [], innerException)
    {
    }

    /// <summary>
    /// Creates a template rendering exception with diagnostics.
    /// </summary>
    public PdfTemplateRenderException(
        string message,
        IEnumerable<PdfTemplateDiagnostic> diagnostics,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Diagnostics = [.. diagnostics];
    }

    /// <summary>
    /// Gets diagnostics associated with the rendering failure.
    /// </summary>
    public IReadOnlyList<PdfTemplateDiagnostic> Diagnostics { get; }
}
