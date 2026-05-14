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
        => Converter.ToPdfAsync(html);
}
