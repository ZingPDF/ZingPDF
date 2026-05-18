![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF

`ZingPDF` is a proprietary .NET 8 PDF library for loading, creating, editing, signing, validating signatures, redacting, and saving PDFs in C#.

It covers the PDF jobs many applications need first: fluent PDF authoring, Liquid HTML template rendering through a companion package, existing-PDF page editing, text extraction, form creation and completion, signing, signature validation, encryption, redaction, metadata updates, and rewritten saves without prior incremental history.

## Installation

```bash
dotnet add package ZingPDF
```

## Quick start

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

Create and fill a PDF form:

```csharp
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Syntax.CommonDataStructures;

using var pdf = Pdf.Create();
var page = await pdf.GetPageAsync(1);
var form = await pdf.GetOrCreateFormAsync();

await form.AddTextFieldAsync(new TextFormFieldCreationOptions
{
    Name = "Customer.Name",
    Page = page,
    Bounds = Rectangle.FromCoordinates(
        new Coordinate(48, 720),
        new Coordinate(280, 752))
});

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
- edit existing PDFs with `pdf.Pages(...)`
- append, insert, delete, export, merge, or split pages
- add text, images, vector drawing, and watermarks to pages
- register standard PDF fonts and embedded TrueType fonts
- create, fill, flatten, sign, and validate AcroForm signature fields
- read and update metadata
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

## Package split

- `ZingPDF`: core PDF load, author, edit, sign, signature validation, redact, form, metadata, and encryption APIs
- `ZingPDF.GoogleFonts`: download and register Google Fonts
- `ZingPDF.OCR`: OCR support for scanned and image-based PDF pages
- `ZingPDF.FromHTML`: render HTML to PDF through PuppeteerSharp
- `ZingPDF.Templates`: shared contracts for template renderer packages
- `ZingPDF.Templates.LiquidHtml`: render Liquid HTML templates to PDF through Fluid and `ZingPDF.FromHTML`

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.
