namespace ZingPDF.Templates;

/// <summary>
/// Reads PDF template text from a file.
/// </summary>
public sealed class FilePdfTemplateSource : PdfTemplateSource
{
    private readonly string _path;

    /// <summary>
    /// Creates a file-backed template source.
    /// </summary>
    public FilePdfTemplateSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _path = Path.GetFullPath(path);
    }

    /// <inheritdoc />
    public override string DisplayName => _path;

    /// <inheritdoc />
    public override string? BasePath => Path.GetDirectoryName(_path);

    /// <inheritdoc />
    public override Task<string> ReadAsync(CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(_path, cancellationToken);
}
