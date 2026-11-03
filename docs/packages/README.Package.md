![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF

`ZingPDF` is a proprietary PDF library for .NET 8 and later for loading, creating, editing, rendering, signing, validating signatures, redacting, and saving PDFs in C#.

It covers the PDF jobs many applications need first: fluent PDF authoring, Liquid HTML template rendering through a companion package, existing-PDF page editing, page-to-PNG rendering, text extraction, form creation and completion, signing, signature validation, encryption, redaction, metadata updates, and rewritten saves without prior incremental history.


## .NET compatibility

Targets `net8.0`, with .NET 8 as the minimum. Runtime validation covers .NET 8,
9, and 10 on Windows. .NET Framework 4.x and applications targeting .NET 6 or 7
are not supported. Native dependencies and deployment requirements remain package-specific. See [runtime and platform requirements](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md).

## Installation

```bash
dotnet add package ZingPDF
```

## Quick start

The core package depends on ImageSharp 3.2.0. Building ZingPDF itself from source
requires a valid SixLabors licence. Consuming the compiled ZingPDF package does
not directly invoke ImageSharp's build-time validation unless your application
also directly references ImageSharp. See [source-build setup](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/WORKFLOWS.md#sixlabors-build-licence).

Create a new PDF with the fluent API:

```csharp
using ZingPDF;
using ZingPDF.Graphics;

await Pdf.New()
    .Page(page => page
        .Size(595, 842)
        .Text(text => text
            .Value("Hello from ZingPDF")
            .HelveticaBold()
            .FontSize(24)
            .At(48, 780))
        .Rectangle(box => box
            .At(48, 720)
            .Size(220, 48)
            .Fill(RGBColour.PrimaryBlue)))
    .SaveAsync(File.Create("hello.pdf"));
```

Edit an existing PDF:

```csharp
using var input = File.OpenRead("input.pdf");
using var output = File.Create("edited.pdf");
using var pdf = Pdf.Load(input);

await pdf.Pages(pages => pages
    .Page(1, page => page
        .Text(text => text
            .Value("Approved")
            .HelveticaBold()
            .FontSize(18)
            .At(48, 780))))
    .SaveAsync(output);
```

Replace a supported existing line:

```csharp
var editor = pdf.CreateTextEditor();
var line = (await editor.GetLinesAsync(1)).First(); // choose by text/bounds in your UI
var check = await editor.CheckReplacementAsync(line, "Corrected wording");
if (!check.CanApply)
    throw new InvalidOperationException(check.Message);
await editor.ReplaceLineAsync(line, "Corrected wording");
```

The line keeps its original baseline and painting state. Supply `PdfTextReplacementOptions.TrueTypeFontData`
when another font is needed; embedding and Unicode mappings are automatic. Coverage and available
width are checked before mutation. Obtain fresh line handles after content changes. This is
single-baseline editing, without paragraph reflow or complex-script shaping. It follows incremental
save semantics and is not secure redaction. See [text editing and limits](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/TEXT_EDITING.md).

Author Unicode text without building PDF font dictionaries:

```csharp
var font = await pdf.RegisterUnicodeTrueTypeFontAsync("NotoSans-Regular.ttf");
var page = await pdf.GetPageAsync(1);
await page.AddTextAsync("Greek: \u03A9\nCyrillic: \u0416", new Coordinate(40, 700),
    font, 14, RGBColour.Black, new ZingPDF.Text.TextPlacementOptions { LineHeight = 18 });
var missing = font.GetMissingCharacters("More wording");
var width = font.MeasureText("More wording", 14);
```

Register once per document; character mappings expand as text is added. Font streams remain
caller-owned. Existing `RegisterTrueTypeFontAsync` retains WinAnsi behaviour. Unicode authoring
checks glyph coverage but does not perform shaping, kerning, fallback or font subsetting.
Use `WithUnicodeTrueTypeFont` in fluent authoring. Existing-line options can also take
`Font = font` instead of raw font bytes. See [editor-facing APIs](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/TEXT_EDITING.md).

`MovePageAsync(from, to)` reorders final 1-based page indices while retaining page identities,
geometry, resources and widgets. `IsEncryptedAsync()` avoids trailer inspection and includes
queued encryption/removal settings. High-level page additions isolate existing graphics state;
font resource collisions are assigned local aliases automatically.

Create a blank PDF with the standard page API:

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

await pdf.SaveAsync(File.Create("hello-standard.pdf"));
```

Read displayed page geometry and translate an overlay point:

```csharp
using ZingPDF;
using ZingPDF.Elements.Drawing;

using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

var page = await pdf.GetPageAsync(1);
var geometry = await page.GetGeometryAsync();
var displayPoint = geometry.PageToDisplay(new Coordinate(72, 144));

Console.WriteLine($"{geometry.DisplayWidth} x {geometry.DisplayHeight}");
Console.WriteLine($"Preview point: {displayPoint.X}, {displayPoint.Y}");
```

`GetGeometryAsync()` resolves inherited `MediaBox`, `CropBox`, and `Rotate` values. PDF page coordinates use a bottom-left origin. Display coordinates use a top-left origin after the visible page box and clockwise page rotation are applied.

Render a page preview to PNG:

```csharp
using ZingPDF;
using ZingPDF.Rendering;

using var input = File.OpenRead("input.pdf");
using var pdf = Pdf.Load(input);

var preview = await pdf.RenderPageAsync(1, new PdfPageRenderOptions
{
    Scale = 1.5
});

await File.WriteAllBytesAsync("page-1.png", preview.PngBytes.ToArray());
Console.WriteLine($"{preview.PixelWidth} x {preview.PixelHeight}");
```

Page rendering uses the current in-memory PDF state, so unsaved page edits can be previewed before `SaveAsync(...)`. Rendering is backed by PDFium through `PDFtoImage` and `SkiaSharp` native assets for Windows, Linux, and macOS.

For validation tests, render small vector-only pages and compare sampled pixels or approved image signatures. Use `PdfPageGeometry.PageToDisplay(...)` to convert PDF coordinates into expected display pixels for overlay and alignment checks. Rendering benchmarks live in `tests/ZingPDF.Performance`.

Create and fill a PDF form:

```csharp
using ZingPDF;
using ZingPDF.Elements.Drawing;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Syntax.CommonDataStructures;

using var pdf = Pdf.Create();
var form = await pdf.GetOrCreateFormAsync();

await form.AddTextFieldAsync(
    1,
    "Customer.Name",
    Rectangle.FromCoordinates(
        new Coordinate(48, 720),
        new Coordinate(280, 752)),
    options => options.DefaultValue = "Ada Lovelace");

var nameField = await form.GetFieldAsync<TextFormField>("Customer.Name");
await nameField.SetValueAsync("Ada Lovelace");

await pdf.SaveAsync(File.Create("form-created-and-filled.pdf"));
```

Redact sensitive text and save rewritten output:

```csharp
using var input = File.OpenRead("sensitive.pdf");
using var output = File.Create("redacted.pdf");
using var pdf = Pdf.Load(input);

var plan = await pdf.RedactionAsync();
await plan.MarkTextAsync("Secret");
await plan.ApplyAsync(new PdfRedactionOptions
{
    OverlayText = "REDACTED"
});

await pdf.SaveAsync(output);
```

Sign a visible signature field:

```csharp
using System.Security.Cryptography.X509Certificates;
using ZingPDF.Elements.Forms.FieldTypes.Signature;

using var certificate = new X509Certificate2("signing.pfx", "password");
using var input = File.OpenRead("contract.pdf");
using var output = File.Create("contract-signed.pdf");
using var pdf = Pdf.Load(input);

var form = await pdf.GetFormAsync();
var signatureField = await form.GetFieldAsync<SignatureFormField>("Approval.Signature");

await signatureField.SignAsync(certificate, new PdfSignatureOptions
{
    SignerName = "Ada Lovelace",
    Reason = "Approved"
});

await pdf.SaveAsync(output);
```

Validate a signed PDF:

```csharp
using ZingPDF;

using var input = File.OpenRead("contract-signed.pdf");
using var pdf = Pdf.Load(input);

var signatures = await pdf.GetSignaturesAsync();
var result = await signatures[0].ValidateIntegrityAsync();

if (result.Status == PdfSignatureValidationStatus.Valid)
{
    Console.WriteLine("The signed byte ranges match the detached CMS signature.");
}
```

## Main workflows

- create new PDFs with `Pdf.New()` or `Pdf.Create()`
- create PDFs from Liquid HTML templates with `ZingPDF.Templates.LiquidHtml`
- convert HTML strings and URLs to PDF with `ZingPDF.FromHTML`
- edit existing PDFs with `pdf.Pages(...)`
- append, insert, delete, export, merge, or split pages
- read cropped and rotated page display geometry and convert overlay coordinates
- render pages to PNG bytes for previews or thumbnails
- add text, images, vector drawing, and watermarks to pages
- register standard PDF fonts and embedded TrueType fonts
- create, fill, flatten, sign, and validate AcroForm fields
- inspect push-button captions and action metadata
- read and update metadata
- inspect the catalog, trailer, page tree, and indirect objects through `pdf.Objects`
- trace named parser and document-workflow timings with `PerformanceTrace`
- extract text from full documents or individual pages
- redact exact text matches or explicit regions with rewritten-file output
- compress output and tune image quality
- decrypt, encrypt, restrict permissions, and rewrite PDFs without prior incremental history

## Documentation

- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
- docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
- performance: [zingpdf.dev/performance.html](https://zingpdf.dev/performance.html)
- API reference: [zingpdf.dev/api/](https://zingpdf.dev/api/)
- examples folder: [github.com/ZingPDF/ZingPDF/tree/main/examples](https://github.com/ZingPDF/ZingPDF/tree/main/examples)
- HTML conversion guide: [zingpdf.dev/convert-html-to-pdf-csharp.html](https://zingpdf.dev/convert-html-to-pdf-csharp.html)
- form creation guide: [zingpdf.dev/create-pdf-form-fields-csharp.html](https://zingpdf.dev/create-pdf-form-fields-csharp.html)
- push-button metadata guide: [zingpdf.dev/inspect-pdf-push-button-actions-csharp.html](https://zingpdf.dev/inspect-pdf-push-button-actions-csharp.html)
- object access guide: [zingpdf.dev/inspect-pdf-object-graph-csharp.html](https://zingpdf.dev/inspect-pdf-object-graph-csharp.html)
- performance tracing guide: [zingpdf.dev/trace-pdf-performance-csharp.html](https://zingpdf.dev/trace-pdf-performance-csharp.html)

## Package split

- `ZingPDF`: core PDF load, author, edit, page rendering, sign, signature validation, redact, form, metadata, and encryption APIs
- `ZingPDF.GoogleFonts`: download and register Google Fonts
- `ZingPDF.OCR`: OCR support for scanned and image-based PDF pages
- `ZingPDF.FromHTML`: render HTML to PDF through PuppeteerSharp
- `ZingPDF.Templates`: shared contracts for template renderer packages
- `ZingPDF.Templates.LiquidHtml`: render Liquid HTML templates to PDF through Fluid and `ZingPDF.FromHTML`

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have paid license coverage with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling. Versions released during a paid subscription remain licensed within the purchased scope after it ends; new versions and support require an active subscription.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.
