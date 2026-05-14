namespace ZingPDF.Templates;

/// <summary>
/// Reads PDF template text from an in-memory string.
/// </summary>
public sealed class StringPdfTemplateSource : PdfTemplateSource
{
    private readonly string _template;

    /// <summary>
    /// Creates a string-backed template source.
    /// </summary>
    public StringPdfTemplateSource(string template, string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        _template = template;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "inline template" : displayName;
    }

    /// <inheritdoc />
    public override string DisplayName { get; }

    /// <inheritdoc />
    public override Task<string> ReadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_template);
}
