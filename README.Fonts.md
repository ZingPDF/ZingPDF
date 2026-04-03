# ZingPDF.Fonts

`ZingPDF.Fonts` contains the font metadata, metrics, and loading helpers used by `ZingPDF`.

## Installation

If you are consuming the main `ZingPDF` package directly, this package is restored automatically as a dependency.

To reference it explicitly:

```bash
dotnet add package ZingPDF.Fonts
```

## Purpose

This package provides:

- PDF standard font metrics
- TrueType font loading helpers for embedded font registration
- embedded font metrics helpers
- simple font metrics providers used by the main library

## Typical usage

`ZingPDF.Fonts` is used by the core `ZingPDF` package to support:

- registration of standard PDF fonts
- registration of embedded TrueType fonts from files or streams
- text measurement for forms and other layout calculations

The document-level registration APIs live in the main `ZingPDF` package:

```csharp
using var pdf = Pdf.Create();

var helvetica = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
var custom = await pdf.RegisterTrueTypeFontAsync("MyFont.ttf");
```

High-level text registration currently targets WinAnsi / Windows-1252 text workflows.

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

## Support and compatibility

See `SUPPORT.md` in the package root or the repository for the current support stance and release-readiness notes.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
