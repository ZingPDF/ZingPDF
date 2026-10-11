using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Rendering;

namespace ZingPDF.OCR;

/// <summary>
/// OCR helper methods for PDF pages using embedded text, image candidates, or rendered page images.
/// </summary>
public static class PdfOcrExtensions
{
    /// <summary>Extracts text from one 1-based page using the selected embedded-text or OCR input source.</summary>
    /// <remarks>Rendered-page input follows visible crop and page rotation. Input coverage does not certify text recognition completeness.</remarks>
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
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Mode == PdfOcrMode.ImageCandidate && options.PreferEmbeddedText)
        {
            var embeddedText = await pdf.ExtractTextAsync(pageNumber, new TextExtractionOptions
            {
                OutputKind = TextExtractionOutputKind.PlainText
            });
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(embeddedText.PlainText))
            {
                return new OcrPageResult
                {
                    PageNumber = pageNumber,
                    Text = embeddedText.PlainText!,
                    UsedEmbeddedText = true,
                    UsedOcr = false,
                    Source = OcrPageSource.EmbeddedText,
                    InputCoverage = OcrInputCoverage.None
                };
            }
        }

        OcrInputImage? image;
        var inputCoverage = OcrInputCoverage.None;
        var source = OcrPageSource.None;
        if (options.Mode == PdfOcrMode.RenderedPage)
        {
            var page = await pdf.GetPageAsync(pageNumber);
            cancellationToken.ThrowIfCancellationRequested();
            var geometry = await page.GetGeometryAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ValidateRenderedPixelCount(pageNumber, geometry.DisplayWidth, geometry.DisplayHeight, options);

#pragma warning disable CA1416 // RenderPageAsync defines the supported platform set used by this opt-in branch.
            var rendered = await pdf.RenderPageAsync(pageNumber, new PdfPageRenderOptions
            {
                Scale = options.Dpi / 72d,
                ApplyPageRotation = true,
                UseVisibleBox = true
            }, cancellationToken);
#pragma warning restore CA1416
            cancellationToken.ThrowIfCancellationRequested();
            if (rendered.PageNumber != pageNumber)
            {
                throw new InvalidOperationException($"The renderer returned page {rendered.PageNumber} for requested page {pageNumber}.");
            }

            if (rendered.PixelWidth <= 0 || rendered.PixelHeight <= 0 || rendered.PngBytes.IsEmpty)
            {
                throw new InvalidOperationException($"The renderer returned an empty image for page {pageNumber}.");
            }

            ValidateRenderedDimensions(pageNumber, rendered.PixelWidth, rendered.PixelHeight, options);

            image = new OcrInputImage
            {
                PageNumber = pageNumber,
                Width = rendered.PixelWidth,
                Height = rendered.PixelHeight,
                MimeType = "image/png",
                Data = rendered.PngBytes.ToArray()
            };
            inputCoverage = OcrInputCoverage.RenderedPage;
            source = OcrPageSource.RenderedPage;
        }
        else
        {
            var page = await pdf.GetPageAsync(pageNumber);
            cancellationToken.ThrowIfCancellationRequested();
            image = await PageImageExtractor.TryExtractBestCandidateAsync(page, pdf, cancellationToken);
            if (image is not null)
            {
                inputCoverage = OcrInputCoverage.ImageCandidate;
                source = OcrPageSource.ImageCandidate;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
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
                UsedOcr = false,
                Source = OcrPageSource.None,
                InputCoverage = OcrInputCoverage.None
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
        var text = await engine.RecognizeAsync(new OcrInputImage
        {
            PageNumber = pageNumber,
            Width = image.Width,
            Height = image.Height,
            MimeType = image.MimeType,
            Data = image.Data
        }, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new OcrPageResult
        {
            PageNumber = pageNumber,
            Text = text,
            UsedEmbeddedText = false,
            UsedOcr = true,
            Source = source,
            InputCoverage = inputCoverage,
            SourceImageMimeType = image.MimeType,
            SourceImageWidth = image.Width,
            SourceImageHeight = image.Height
        };
    }

    private static void ValidateRenderedPixelCount(int pageNumber, double width, double height, PdfOcrOptions options)
    {
        var scale = options.Dpi / 72d;
        var pixelWidth = Math.Round(width * scale, MidpointRounding.AwayFromZero);
        var pixelHeight = Math.Round(height * scale, MidpointRounding.AwayFromZero);
        if (!double.IsFinite(pixelWidth) || !double.IsFinite(pixelHeight)
            || pixelWidth < 1 || pixelHeight < 1
            || pixelWidth > int.MaxValue || pixelHeight > int.MaxValue
            || pixelWidth * pixelHeight > options.MaxPixelCount)
        {
            throw new InvalidOperationException(
                $"Page {pageNumber} exceeds the rendered OCR image limit of {options.MaxPixelCount} pixels at {options.Dpi} DPI.");
        }
    }

    private static void ValidateRenderedDimensions(int pageNumber, int width, int height, PdfOcrOptions options)
    {
        if ((long)width * height > options.MaxPixelCount)
        {
            throw new InvalidOperationException(
                $"Page {pageNumber} renderer output exceeds the OCR image limit of {options.MaxPixelCount} pixels.");
        }
    }

    /// <summary>Extracts text from each page in the document, returning results in 1-based page order.</summary>
    public static async Task<IReadOnlyList<OcrPageResult>> ExtractTextWithOcrAsync(
        this IPdf pdf,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(engine);

        options ??= new PdfOcrOptions();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var pageCount = await pdf.GetPageCountAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<OcrPageResult>(pageCount);

        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await pdf.ExtractTextWithOcrAsync(pageNumber, engine, options, cancellationToken));
        }

        return results;
    }

    /// <summary>Extracts text from every page and joins non-empty page results with the platform newline.</summary>
    public static async Task<string> ExtractPlainTextWithOcrAsync(
        this IPdf pdf,
        IOcrEngine engine,
        PdfOcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var pages = await pdf.ExtractTextWithOcrAsync(engine, options, cancellationToken);
        return string.Join(Environment.NewLine, pages.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    /// <summary>Extracts text from one 1-based page and returns only the text value.</summary>
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
