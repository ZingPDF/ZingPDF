# ZingPDF.OCR

`ZingPDF.OCR` adds OCR support for scanned and image-based PDF pages.

Use it when:

- a PDF page has little or no embedded text
- the page is primarily a scanned image
- you want OCR as part of the main extraction workflow

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

## What it does

- can prefer embedded PDF text or OCR depending on the selected options
- works best on image-based pages
- picks the largest image XObject on a page as the OCR candidate

## Current limits

- this package does not render arbitrary PDF drawing commands into an OCR image
- OCR works on image-based pages and other pages with usable image XObjects
- JPEG, JPEG 2000 passthrough, and common 8-bit RGB or grayscale image streams are the main supported inputs today
- `TesseractOcrEngine` requires native Tesseract support and language data files at runtime

## Related docs

- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
