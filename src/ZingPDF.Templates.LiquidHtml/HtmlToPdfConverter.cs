using ZingPDF.FromHTML;

namespace ZingPDF.Templates.LiquidHtml;

/// <summary>
/// Converts rendered HTML into a PDF stream.
/// </summary>
public interface IHtmlToPdfConverter
{
    /// <summary>
    /// Converts the supplied HTML document into a PDF stream.
    /// </summary>
    Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default);
}

internal sealed class ZingPdfHtmlToPdfConverter : IHtmlToPdfConverter
{
    public Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default)
        => Converter.ToPdfAsync(html, cancellationToken);
}

/// <summary>Converts template HTML using a caller-owned reusable Chromium renderer.</summary>
/// <remarks>Dispose the renderer after all templates using it finish. This adapter does not own it.</remarks>
public sealed class RendererHtmlToPdfConverter : IHtmlToPdfConverter
{
    private readonly HtmlToPdfRenderer _renderer;
    /// <summary>Uses the supplied renderer's browser, concurrency, timeout, offline and print settings.</summary>
    public RendererHtmlToPdfConverter(HtmlToPdfRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
    }
    /// <summary>Converts template HTML to an owned PDF stream, forwarding cancellation.</summary>
    public Task<Stream> ConvertAsync(string html, CancellationToken cancellationToken = default)
        => _renderer.RenderAsync(html, cancellationToken);
}
