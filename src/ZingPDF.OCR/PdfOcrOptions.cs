namespace ZingPDF.OCR;

/// <summary>
/// Controls how OCR fallback behaves for a PDF.
/// </summary>
public sealed class PdfOcrOptions
{
    /// <summary>
    /// Gets the image source used for OCR. The default preserves the existing best-image-candidate behavior.
    /// </summary>
    public PdfOcrMode Mode { get; init; } = PdfOcrMode.ImageCandidate;

    /// <summary>
    /// Gets the rendering resolution used when <see cref="Mode"/> is <see cref="PdfOcrMode.RenderedPage"/>.
    /// </summary>
    /// <remarks>The supported range is 72 through 600 dots per inch.</remarks>
    public int Dpi { get; init; } = 300;

    /// <summary>
    /// Gets the maximum number of pixels permitted in a rendered page OCR image.
    /// </summary>
    /// <remarks>The value must be between 1 and 50 million pixels. The default is 50 million pixels.</remarks>
    public long MaxPixelCount { get; init; } = 50_000_000;

    /// <summary>
    /// When true, existing embedded PDF text is returned before OCR is attempted.
    /// </summary>
    /// <remarks>This setting is ignored when <see cref="Mode"/> is <see cref="PdfOcrMode.RenderedPage"/>.</remarks>
    public bool PreferEmbeddedText { get; init; } = true;

    /// <summary>
    /// When true, pages without a supported OCR image candidate throw instead of returning an empty result.
    /// </summary>
    public bool ThrowWhenNoOcrCandidate { get; init; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode));
        }

        if (Dpi is < 72 or > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(Dpi), "Dpi must be between 72 and 600.");
        }

        if (MaxPixelCount is <= 0 or > 50_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxPixelCount), "MaxPixelCount must be between 1 and 50 million pixels.");
        }
    }
}
