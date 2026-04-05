# ZingPDF

`ZingPDF` is a proprietary .NET 8 library for loading, creating, editing, and saving PDFs in C#.

It covers the PDF jobs many applications need first: page editing, document assembly, text extraction, form filling and flattening, watermarking, compression, metadata updates, encryption, and clean rewrites without prior incremental history.

## Installation

```bash
dotnet add package ZingPDF
```

## Quick start

Create a blank PDF, write text, and save it:

```csharp
using ZingPDF;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax.CommonDataStructures;

using var pdf = Pdf.Create();
var page = await pdf.GetPageAsync(1);
var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

await page.AddTextAsync(
    "Hello from ZingPDF",
    Rectangle.FromDimensions(320, 72),
    font,
    18,
    RGBColour.Black);

await pdf.SaveAsync(File.Create("hello.pdf"));
```

Fill and flatten a PDF form:

```csharp
using ZingPDF.Elements.Forms.FieldTypes.Text;

using var input = File.OpenRead("form.pdf");
using var output = File.Create("form-completed.pdf");
using var pdf = Pdf.Load(input);

var form = await pdf.GetFormAsync();
var nameField = await form.GetFieldAsync<TextFormField>("Customer.Name");
await nameField.SetValueAsync("Ada Lovelace");

await form.FlattenAsync();
await pdf.SaveAsync(output);
```

Export selected pages into a new document:

```csharp
using var input = File.OpenRead("packet.pdf");
using var output = File.Create("selected-pages.pdf");
using var pdf = Pdf.Load(input);
using var selectedPages = await pdf.ExportPagesAsync([1, 3, 5]);

await selectedPages.SaveAsync(output);
```

## Main workflows

- create new PDFs and append, insert, delete, export, or split pages
- add text, images, and watermarks to pages
- register standard PDF fonts and embedded TrueType fonts
- read and update metadata
- extract text from full documents or individual pages
- fill and flatten AcroForm fields
- merge PDFs and save combined output
- compress output and tune image quality
- decrypt, encrypt, and rewrite PDFs without prior incremental history

## Documentation

- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
- performance: [zingpdf.dev/performance.html](https://zingpdf.dev/performance.html)
- API reference: [zingpdf.dev/api/](https://zingpdf.dev/api/)
- examples folder: [github.com/ZingPDF/ZingPDF/tree/main/examples](https://github.com/ZingPDF/ZingPDF/tree/main/examples)

## Package split

- `ZingPDF`: core PDF load, edit, save, page, text, form, metadata, and encryption APIs
- `ZingPDF.GoogleFonts`: download and register Google Fonts
- `ZingPDF.FromHTML`: render HTML to PDF through PuppeteerSharp
- `ZingPDF.Fonts`: supporting font package used by the main library

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.
