![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.OCR

`ZingPDF.OCR` supplies image-candidate OCR and an explicit rendered-page OCR mode for mixed text, scans and tiled images.

## Installation

```bash
dotnet add package ZingPDF.OCR
```

The built-in `TesseractOcrEngine` also needs Tesseract language data at runtime. See the
[official Tesseract documentation](https://tesseract-ocr.github.io/tessdoc/) for setup details.

## Quick start

```csharp
using ZingPDF;
using ZingPDF.OCR;

using var pdf = Pdf.Load(File.OpenRead("scanned.pdf"));
var engine = new TesseractOcrEngine("./tessdata", "eng");

var text = await pdf.ExtractPlainTextWithOcrAsync(engine);
```

This preserves the default: return existing nonblank embedded text, otherwise OCR the largest supported image XObject. A digital header can therefore bypass a scanned body, and one candidate can omit other image tiles. Use the rendered-page mode for visible-page input coverage:

```csharp
var result = await pdf.ExtractTextWithOcrAsync(1, engine, new PdfOcrOptions
{
    Mode = PdfOcrMode.RenderedPage,
    Dpi = 300,
    MaxPixelCount = 50_000_000
}, cancellationToken);

Console.WriteLine(result.Source);
Console.WriteLine(result.InputCoverage);
```

Rendered-page mode uses PDFium to produce a PNG of the visible crop/media intersection with page rotation and annotations. It ignores `PreferEmbeddedText` and sends that rendered image to the selected `IOcrEngine`. `InputCoverage` describes the supplied image, not recognition accuracy or recovered-text completeness. `Source` distinguishes embedded text, a single image candidate, a rendered page and no input.

## Main workflows

- extract OCR text from scanned or image-based PDF pages
- combine OCR with the main text extraction workflow
- use the built-in `TesseractOcrEngine` or a custom `IOcrEngine`

## Current limits

- image-candidate mode processes one usable image XObject; it does not cover arbitrary page drawing commands or multiple scan tiles
- JPEG, JPEG 2000 passthrough, and common 8-bit RGB or grayscale streams are supported candidate inputs; rendered-page mode uses PNG output from the page renderer
- rendered-page mode requires the native PDFium rendering dependencies on the deployed platform; install and validate them before disabling runtime network access
- rendering resolution is 72–600 DPI; the default is 300 DPI and the default rendered-image limit is 50 million pixels
- cancellation is checked between operations and passed to the engine; native rendering and in-process OCR are not guaranteed to stop immediately after cancellation, so enforce hard job deadlines in a separately supervised worker
- `TesseractOcrEngine` requires native Tesseract support and language data files at runtime

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have paid license coverage with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling. Versions released during a paid subscription remain licensed within the purchased scope after it ends; new versions and support require an active subscription.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.

## Related docs

- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
