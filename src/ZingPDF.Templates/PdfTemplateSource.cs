namespace ZingPDF.Templates;

/// <summary>
/// Represents the source content for a PDF template.
/// </summary>
public abstract class PdfTemplateSource
{
    /// <summary>
    /// Gets a display name for diagnostics.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets the base path used to resolve relative template assets and includes, when known.
    /// </summary>
    public virtual string? BasePath => null;

    /// <summary>
    /// Reads the template text.
    /// </summary>
    public abstract Task<string> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a template source from a file path.
    /// </summary>
    public static PdfTemplateSource FromFile(string path)
        => new FilePdfTemplateSource(path);

    /// <summary>
    /// Creates a template source from an in-memory string.
    /// </summary>
    public static PdfTemplateSource FromString(string template, string? displayName = null)
        => new StringPdfTemplateSource(template, displayName);
}
