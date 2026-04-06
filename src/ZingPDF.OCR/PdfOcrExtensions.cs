using ZingPDF.Elements.Drawing.Text.Extraction;

namespace ZingPDF.OCR;

/// <summary>
/// OCR helper methods for image-based PDF pages.
/// </summary>
public static class PdfOcrExtensions
{
    public static async Task<OcrPageResult> ExtractTextWithOcrAsync(
        this IPdf pdf,
        int pageNumber,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(engine);

        options ??= new PdfOcrOptions();

        if (options.PreferEmbeddedText)
        {
            var embeddedText = await pdf.ExtractTextAsync(pageNumber, new TextExtractionOptions
            {
                OutputKind = TextExtractionOutputKind.PlainText
            });

            if (!string.IsNullOrWhiteSpace(embeddedText.PlainText))
            {
                return new OcrPageResult
                {
                    PageNumber = pageNumber,
                    Text = embeddedText.PlainText!,
                    UsedEmbeddedText = true,
                    UsedOcr = false
                };
            }
        }

        var page = await pdf.GetPageAsync(pageNumber);
        var image = await PageImageExtractor.TryExtractBestCandidateAsync(page, pdf, cancellationToken);
        if (image is null)
        {
            if (options.ThrowWhenNoOcrCandidate)
            {
                throw new InvalidOperationException($"Page {pageNumber} does not contain a supported OCR image candidate.");
            }

            return new OcrPageResult
            {
                PageNumber = pageNumber,
                Text = string.Empty,
                UsedEmbeddedText = false,
                UsedOcr = false
            };
        }

        var text = await engine.RecognizeAsync(new OcrInputImage
        {
            PageNumber = pageNumber,
            Width = image.Width,
            Height = image.Height,
            MimeType = image.MimeType,
            Data = image.Data
        }, cancellationToken);

        return new OcrPageResult
        {
            PageNumber = pageNumber,
            Text = text,
            UsedEmbeddedText = false,
            UsedOcr = true,
            SourceImageMimeType = image.MimeType,
            SourceImageWidth = image.Width,
            SourceImageHeight = image.Height
        };
    }

    public static async Task<IReadOnlyList<OcrPageResult>> ExtractTextWithOcrAsync(
        this IPdf pdf,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(engine);

        var pageCount = await pdf.GetPageCountAsync();
        var results = new List<OcrPageResult>(pageCount);

        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            results.Add(await pdf.ExtractTextWithOcrAsync(pageNumber, engine, options, cancellationToken));
        }

        return results;
    }

    public static async Task<string> ExtractPlainTextWithOcrAsync(
        this IPdf pdf,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var pages = await pdf.ExtractTextWithOcrAsync(engine, options, cancellationToken);
        return string.Join(Environment.NewLine, pages.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public static async Task<string> ExtractPlainTextWithOcrAsync(
        this IPdf pdf,
        int pageNumber,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await pdf.ExtractTextWithOcrAsync(pageNumber, engine, options, cancellationToken);
        return result.Text;
    }
}
