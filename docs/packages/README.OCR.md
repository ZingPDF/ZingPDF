![ZingPDF logomark](logomark.svg)

# ZingPDF.OCR

`ZingPDF.OCR` adds OCR support for scanned and image-based PDF pages.

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

## Main workflows

- extract OCR text from scanned or image-based PDF pages
- combine OCR with the main text extraction workflow
- use the built-in `TesseractOcrEngine` or a custom `IOcrEngine`

## Current limits

- this package does not render arbitrary PDF drawing commands into an OCR image
- OCR works on image-based pages and other pages with usable image XObjects
- JPEG, JPEG 2000 passthrough, and common 8-bit RGB or grayscale image streams are the main supported inputs today
- `TesseractOcrEngine` requires native Tesseract support and language data files at runtime

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.

## Related docs

- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
