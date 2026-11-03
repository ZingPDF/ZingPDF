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
            fontName);
    }

    /// <summary>
    /// Downloads and registers a Unicode TrueType font. Coverage checks, embedding and CID mappings
    /// are handled by ZingPDF. This does not provide script shaping or fallback fonts.
    /// </summary>
    public static async Task<PdfFont> RegisterUnicodeGoogleFontAsync(this IPdf pdf, GoogleFontsClient client,
        GoogleFontRequest request, string? resourceName = null, string? fontName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);
        await using var input = await client.DownloadFontAsync(request, cancellationToken);
        return await pdf.RegisterUnicodeTrueTypeFontAsync(input, resourceName, fontName);
    }

    /// <summary>Configures fluent authoring to use an automatically embedded Unicode Google font.</summary>
    public static PdfAuthoringBuilder.PdfTextAuthoringBuilder WithUnicodeGoogleFont(this PdfAuthoringBuilder.PdfTextAuthoringBuilder text,
        GoogleFontsClient client, GoogleFontRequest request, string? resourceName = null, string? fontName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);
        var key = string.Join("|", "unicode-google-font", request.Family, request.Variant, request.PreferVariableFont, resourceName, fontName);
        return text.Font(pdf => pdf.RegisterUnicodeGoogleFontAsync(client, request, resourceName, fontName, cancellationToken), key);
    }

    /// <summary>
    /// Configures a fluent authoring text builder to use a Google Font.
    /// </summary>
    public static PdfAuthoringBuilder.PdfTextAuthoringBuilder WithGoogleFont(
        this PdfAuthoringBuilder.PdfTextAuthoringBuilder text,
        GoogleFontsClient client,
        GoogleFontRequest request,
        string? resourceName = null,
        string? fontName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        var cacheKey = string.Join(
            "|",
            "google-font",
            request.Family,
            request.Variant,
            request.PreferVariableFont,
            resourceName ?? string.Empty,
            fontName ?? string.Empty);

        return text.Font(
            pdf => pdf.RegisterGoogleFontAsync(
                client,
                request,
                resourceName,
                fontName,
                cancellationToken),
            cacheKey);
    }
}
