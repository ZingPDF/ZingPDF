![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.GoogleFonts

`ZingPDF.GoogleFonts` resolves Google Fonts through the Google Fonts Developer API and registers them into a `ZingPDF` document as embedded TrueType fonts.

## Installation

```bash
dotnet add package ZingPDF.GoogleFonts
```

## Quick start

```csharp
using ZingPDF;
using ZingPDF.GoogleFonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;

using var pdf = Pdf.Create();
var page = await pdf.GetPageAsync(1);

var client = new GoogleFontsClient("<google-fonts-api-key>");
var font = await pdf.RegisterGoogleFontAsync(
    client,
    new GoogleFontRequest
    {
        Family = "Inter",
        Variant = "regular"
    });

await page.AddTextAsync("Hello from Google Fonts", Rectangle.FromDimensions(300, 80), font, 18, RGBColour.Black);
```

## Main workflows

- download and register Google Fonts into a PDF document
- use Google Fonts from the standard page API or the fluent authoring API
- embed the resolved font into the saved PDF

## Notes

- This package requires a Google Fonts Developer API key.
- High-level text registration currently targets WinAnsi / Windows-1252 text workflows.
- Most applications should reference this package alongside `ZingPDF`, not instead of it.

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
