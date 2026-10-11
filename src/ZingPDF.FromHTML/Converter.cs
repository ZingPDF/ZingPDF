using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ZingPDF.FromHTML;

/// <summary>One-shot conversion helpers. Use HtmlToPdfRenderer to reuse a configured browser across jobs.</summary>
public static class Converter
{
    /// <summary>Converts HTML using the original screen-media/default-print settings and automatic browser provisioning.</summary>
    public static Task<Stream> ToPdfAsync(string htmlContent)
        => ToPdfAsync(htmlContent, CancellationToken.None);

    /// <summary>Converts HTML using the original settings, with cancellation and font-readiness waiting.</summary>
    public static Task<Stream> ToPdfAsync(string htmlContent, CancellationToken cancellationToken)
        => ToPdfAsync(htmlContent, LegacyOptions(), cancellationToken);

    /// <summary>Converts HTML with explicit browser/print settings. The returned PDF stream is owned by the caller.</summary>
    public static async Task<Stream> ToPdfAsync(string htmlContent, HtmlToPdfOptions options, CancellationToken cancellationToken = default)
    {
        await using var renderer = new HtmlToPdfRenderer(options);
        return await renderer.RenderAsync(htmlContent, cancellationToken);
    }

    /// <summary>Converts a URL using the original settings and optional navigation wait states.</summary>
    public static Task<Stream> ToPdfAsync(Uri uri, NavigationOptions? navigationOptions = null)
        => ToPdfAsync(uri, navigationOptions, CancellationToken.None);

    /// <summary>Converts a URL using the original settings, optional navigation wait states and cancellation.</summary>
    public static async Task<Stream> ToPdfAsync(Uri uri, NavigationOptions? navigationOptions, CancellationToken cancellationToken)
    {
        await using var renderer = new HtmlToPdfRenderer(LegacyOptions());
        return await renderer.RenderAsync(uri, navigationOptions, cancellationToken);
    }

    private static HtmlToPdfOptions LegacyOptions() => new()
    {
        AllowBrowserDownload = true,
        RenderTimeout = Timeout.InfiniteTimeSpan,
        MediaType = MediaType.Screen,
        PdfOptions = new PdfOptions()
    };
}
