![ZingPDF logomark](https://raw.githubusercontent.com/ZingPDF/ZingPDF/main/docs/packages/logomark.svg)

# ZingPDF.FromHTML

`ZingPDF.FromHTML` provides HTML-to-PDF conversion helpers for ZingPDF using `PuppeteerSharp`.

## Installation

```bash
dotnet add package ZingPDF.FromHTML
```

## Reuse an installed browser

Set `ZINGPDF_BROWSER_PATH` to an installed browser executable. The example receives `cancellationToken` from the application.

```csharp
using ZingPDF.FromHTML;

await using var renderer = new HtmlToPdfRenderer(new HtmlToPdfOptions
{
    BrowserExecutablePath = Environment.GetEnvironmentVariable("ZINGPDF_BROWSER_PATH")
        ?? throw new InvalidOperationException("Set ZINGPDF_BROWSER_PATH to an installed browser."),
    MaxConcurrency = 4,
    RenderTimeout = TimeSpan.FromSeconds(30)
});

await using var pdfStream = await renderer.RenderAsync("""
<!doctype html>
<html>
<head><style>@page { size: A4; margin: 15mm; }</style></head>
<body><h1>Invoice INV-1001</h1></body>
</html>
""", cancellationToken);

await using var output = File.Create("invoice.pdf");
await pdfStream.CopyToAsync(output, cancellationToken);
```

For any absolute URL supported by Chromium, including HTTP, HTTPS, file, data and about URLs, call `renderer.RenderAsync(uri, navigationOptions, cancellationToken)`. `NavigationOptions` sets URL wait states and its navigation timeout (zero disables that timeout); a finite renderer deadline still covers the entire conversion.

## Browser and print settings

- Supply `BrowserExecutablePath` for an installed Chromium-compatible browser. `AllowBrowserDownload` defaults to `false`; enable it explicitly only if runtime downloading is intended.
- Reuse one `HtmlToPdfRenderer` across jobs. It owns one browser and creates an isolated browser context for each conversion. `MaxConcurrency` defaults to 4; additional jobs queue.
- `RenderTimeout` defaults to 30 seconds and covers queueing, browser startup, page loading, font readiness and PDF printing. Set it to `Timeout.InfiniteTimeSpan` to disable the overall deadline. `BrowserStartupTimeout` independently limits browser provisioning and launch to 30 seconds by default and must remain finite. Expiry throws `TimeoutException`; caller cancellation throws `OperationCanceledException`. Context creation and cleanup must complete to release resources; browser startup or downloading can remain renderer-owned after a caller stops waiting, within the separate startup budget. Disposal cancels browser provisioning. The deadline is not an OS-enforced hard worker bound.
- The renderer snapshots options, including print options and launch arguments, at construction. The caller disposes the renderer and returned streams; templates and their converter adapter do not own the renderer.
- New renderer defaults use print media, background printing and CSS page sizes. Set `PdfOptions` for paper, margins, page ranges or headers/footers. Original static `Converter.ToPdfAsync(...)` overloads retain automatic browser downloading, screen media and default PuppeteerSharp print settings. They disable the overall conversion deadline so explicit navigation timeouts, including zero, remain effective; the independent browser startup budget still defaults to 30 seconds. The overload taking `HtmlToPdfOptions` uses explicit settings for one job.
- `Offline = true` emulates offline page networking. Embed fonts and images as data URLs. This setting is not an OS-level network sandbox or a security guarantee.
- HTML and scripts must be trusted. Isolated browser contexts separate job state; they do not sandbox hostile code at the process or network boundary. The renderer does not disable Chromium's sandbox by default.

## Invoice pagination example

[examples/GenerateInvoices](https://github.com/ZingPDF/ZingPDF/tree/main/examples/GenerateInvoices) contains a Liquid invoice template and a Linux container recipe. It exercises 0, 1, 40 and 500 line items, repeated table headers, row preservation, totals and embedded fonts. Browser packages, fonts and OS libraries must be installed during deployment. Validate the recipe in the target Linux image.

## Licensing

ZingPDF is proprietary software. Review `LICENSE.txt` and ensure you have paid license coverage with sufficient seats, or another applicable commercial agreement, before commercial use or commercial bundling. Versions released during a paid subscription remain licensed within the purchased scope after it ends; new versions and support require an active subscription.

Evaluation and other non-commercial use are free.

## Support and compatibility

See `SUPPORT.md` in the package root or [docs/project/SUPPORT.md](https://github.com/ZingPDF/ZingPDF/blob/main/docs/project/SUPPORT.md) in the repository for the current support stance and release-readiness notes.

## More information

- core docs: [zingpdf.dev/docs.html](https://zingpdf.dev/docs.html)
- guides: [zingpdf.dev/guides.html](https://zingpdf.dev/guides.html)
- HTML conversion guide: [zingpdf.dev/convert-html-to-pdf-csharp.html](https://zingpdf.dev/convert-html-to-pdf-csharp.html)
- capability matrix: [zingpdf.dev/capabilities.html](https://zingpdf.dev/capabilities.html)
- repository: [github.com/ZingPDF/ZingPDF](https://github.com/ZingPDF/ZingPDF)
