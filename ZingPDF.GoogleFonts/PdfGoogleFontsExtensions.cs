using ZingPDF.Text;

namespace ZingPDF.GoogleFonts;

/// <summary>
/// Extension methods that bridge Google Fonts with the ZingPDF registration APIs.
/// </summary>
public static class PdfGoogleFontsExtensions
{
    /// <summary>
    /// Downloads a Google Font and registers it as an embedded TrueType font in the PDF.
    /// </summary>
    public static async Task<PdfFont> RegisterGoogleFontAsync(
        this IPdf pdf,
        GoogleFontsClient client,
        GoogleFontRequest request,
        string? resourceName = null,
        string? fontName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        await using var fontStream = await client.DownloadFontAsync(request, cancellationToken);

        return await pdf.RegisterTrueTypeFontAsync(
            fontStream,
            resourceName,
            fontName ?? BuildPdfFontName(request));
    }

    private static string BuildPdfFontName(GoogleFontRequest request)
        => string.Concat($"{request.Family}-{request.Variant}".Where(ch => !char.IsWhiteSpace(ch)));
}
