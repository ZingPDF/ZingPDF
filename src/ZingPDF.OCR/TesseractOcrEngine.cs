using Tesseract;

namespace ZingPDF.OCR;

/// <summary>
/// OCR engine backed by Tesseract.
/// </summary>
public sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly string _dataPath;
    private readonly string _language;
    private readonly EngineMode _engineMode;

    public TesseractOcrEngine(string dataPath, string language = "eng", EngineMode engineMode = EngineMode.Default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        _dataPath = dataPath;
        _language = language;
        _engineMode = engineMode;
    }

    public Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var engine = new TesseractEngine(_dataPath, _language, _engineMode);
            using var pix = Pix.LoadFromMemory(image.Data);
            using var page = engine.Process(pix);

            return (page.GetText() ?? string.Empty).Trim();
        }, cancellationToken);
    }
}
