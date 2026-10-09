![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.Templates.LiquidHtml

`ZingPDF.Templates.LiquidHtml` creates PDFs from Liquid HTML templates. It uses Fluid for Liquid rendering and `ZingPDF.FromHTML` for HTML-to-PDF conversion.

## Installation

```bash
dotnet add package ZingPDF.Templates.LiquidHtml
```

## Quick start

Render a Liquid HTML template file to PDF:

```csharp
using ZingPDF.Templates.LiquidHtml;

var invoice = new
{
    Number = "INV-1001",
    CustomerName = "Ada Lovelace",
    Items = new[]
    {
        new { Description = "Consulting", Total = 240m },
        new { Description = "Support", Total = 80m }
    }
};

await using var output = File.Create("invoice.pdf");

await LiquidHtmlPdfTemplate
    .FromFile("invoice.liquid.html")
    .RenderAsync(invoice, output);
```

The template can use Liquid variables, loops, and conditionals:

```liquid
<!doctype html>
<html>
  <body>
    <h1>Invoice {{ Number }}</h1>
    <p>{{ CustomerName }}</p>

    <table>
      {% for item in Items %}
      <tr>
        <td>{{ item.Description }}</td>
        <td>{{ item.Total }}</td>
      </tr>
      {% endfor %}
    </table>
  </body>
</html>
```

## Share a configured browser

The `invoice` model above can use a caller-owned renderer. Pass the application cancellation token through `RenderAsync`.

```csharp
using ZingPDF.FromHTML;
using ZingPDF.Templates;
using ZingPDF.Templates.LiquidHtml;

await using var renderer = new HtmlToPdfRenderer(new HtmlToPdfOptions
{
    BrowserExecutablePath = Environment.GetEnvironmentVariable("ZINGPDF_BROWSER_PATH")
        ?? throw new InvalidOperationException("Set ZINGPDF_BROWSER_PATH to an installed browser.")
});

var template = LiquidHtmlPdfTemplate.FromSource(
    PdfTemplateSource.FromFile("invoice.liquid.html"),
    new RendererHtmlToPdfConverter(renderer));

await using var output = File.Create("invoice.pdf");
await template.RenderAsync(invoice, output, cancellationToken: cancellationToken);
```

The converter receives the cancellation token after Liquid rendering and uses the renderer's concurrency, deadline and print settings. The renderer deadline starts at browser conversion; it does not include Liquid rendering. Liquid parsing and rendering are not an interruptible OS-level workload.

For paginated invoices, define `@page`, a `thead` with `display: table-header-group`, and `break-inside: avoid` on rows. Keep totals outside repeating table footers. See [examples/GenerateInvoices](https://github.com/ZingPDF/ZingPDF/tree/main/examples/GenerateInvoices) for 0, 1, 40 and 500 line items, repeated headers, totals and embedded fonts.

## Notes

- Supply `BrowserExecutablePath` for an installed Chromium-compatible browser. `AllowBrowserDownload` defaults to `false`; enable it explicitly only if runtime downloading is intended.
- Reuse one `HtmlToPdfRenderer` across jobs. It owns one browser and creates an isolated browser context for each conversion. `MaxConcurrency` defaults to 4; additional jobs queue.
- `RenderTimeout` defaults to 30 seconds and covers queueing, browser startup, page loading, font readiness and PDF printing. Set it to `Timeout.InfiniteTimeSpan` to disable the overall deadline. `BrowserStartupTimeout` independently limits browser provisioning and launch to 30 seconds by default and must remain finite. Expiry throws `TimeoutException`; caller cancellation throws `OperationCanceledException`. Context creation and cleanup must complete to release resources; browser startup or downloading can remain renderer-owned after a caller stops waiting, within the separate startup budget. Disposal cancels browser provisioning. The deadline is not an OS-enforced hard worker bound.
- The renderer snapshots options, including print options and launch arguments, at construction. The caller disposes the renderer and returned streams; templates and their converter adapter do not own the renderer.
- New renderer defaults use print media, background printing and CSS page sizes. Set `PdfOptions` for paper, margins, page ranges or headers/footers. Original static `Converter.ToPdfAsync(...)` overloads retain automatic browser downloading, screen media and default PuppeteerSharp print settings. They disable the overall conversion deadline so explicit navigation timeouts, including zero, remain effective; the independent browser startup budget still defaults to 30 seconds. The overload taking `HtmlToPdfOptions` uses explicit settings for one job.
- `Offline = true` emulates offline page networking. Embed fonts and images as data URLs. This setting is not an OS-level network sandbox or a security guarantee.
- HTML and scripts must be trusted. Isolated browser contexts separate job state; they do not sandbox hostile code at the process or network boundary. The renderer does not disable Chromium's sandbox by default.
- File-backed templates resolve Liquid includes from the template directory by default.
- Use `RenderHtmlAsync(...)` to inspect rendered HTML before PDF conversion.
- Default `FromFile`, `FromString` and single-argument `FromSource` use the original static converter settings. Select the two-argument `FromSource` overload to control browser deployment and printing.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- Liquid HTML template guide: [zingpdf.dev/create-pdf-from-liquid-html-template-csharp.html](https://zingpdf.dev/create-pdf-from-liquid-html-template-csharp.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
