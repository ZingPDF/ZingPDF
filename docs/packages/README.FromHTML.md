# ZingPDF.FromHTML

`ZingPDF.FromHTML` provides HTML-to-PDF conversion helpers for ZingPDF using `PuppeteerSharp`.

## Installation

```bash
dotnet add package ZingPDF.FromHTML
```

## Quick start

Render an HTML string to a PDF stream:

```csharp
using ZingPDF.FromHTML;

await using var pdfStream = await Converter.ToPdfAsync("""
<!doctype html>
<html>
  <body>
    <h1>Invoice</h1>
    <p>Rendered through Chromium.</p>
  </body>
</html>
""");

await using var output = File.Create("invoice.pdf");
await pdfStream.CopyToAsync(output);
```

Render a URL to PDF:

```csharp
using ZingPDF.FromHTML;

await using var pdfStream = await Converter.ToPdfAsync(new Uri("https://example.com/report"));
await using var output = File.Create("report.pdf");
await pdfStream.CopyToAsync(output);
```

## Notes

- This package depends on `ZingPDF`.
- Runtime behaviour depends on the browser automation environment required by `PuppeteerSharp`.
- It is best treated as an add-on package with additional deployment requirements beyond the core library.

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have an active paid subscription with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
