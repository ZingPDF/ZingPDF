using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using ZingPDF.FromHTML;
using ZingPDF.Templates;
using ZingPDF.Templates.LiquidHtml;

var browserPath = Environment.GetEnvironmentVariable("ZINGPDF_BROWSER_PATH")
    ?? throw new InvalidOperationException("Set ZINGPDF_BROWSER_PATH to a preinstalled Chromium executable; this example never downloads a browser.");
var outputDirectory = Path.GetFullPath(args.FirstOrDefault() ?? "output/invoices");
Directory.CreateDirectory(outputDirectory);
var font = Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "NotoSans-Regular.ttf")));
// Embed the application-controlled asset before parsing Liquid; customer/model text remains HTML-escaped by Fluid.
var source = (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "invoice.liquid.html")))
    .Replace("__FONT_DATA__", font, StringComparison.Ordinal);
await using var renderer = new HtmlToPdfRenderer(new HtmlToPdfOptions
{
    BrowserExecutablePath = browserPath,
    MaxConcurrency = 4,
    RenderTimeout = TimeSpan.FromSeconds(30),
    Offline = true,
    // Container images may explicitly opt out of the sandbox only inside a separately isolated worker.
    LaunchArguments = Environment.GetEnvironmentVariable("ZINGPDF_NO_SANDBOX") == "1" ? ["--no-sandbox"] : [],
    PdfOptions = new PdfOptions { Format = PaperFormat.A4, PrintBackground = true, PreferCSSPageSize = true }
});
var template = LiquidHtmlPdfTemplate.FromSource(PdfTemplateSource.FromString(source), new RendererHtmlToPdfConverter(renderer));
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
var results = new List<object>();
var cases = new[] { ("invoice", 0), ("invoice", 1), ("invoice", 40), ("invoice", 500), ("credit-note", 40), ("account-report", 500) };
foreach (var (kind, count) in cases)
{
    var lines = Enumerable.Range(1, count).Select(i => new InvoiceLine(
        $"LINE{i:0000}",
        i % 7 == 0 ? "Extended service description that wraps across multiple lines while the reference and amount remain on the same page." : "Software service and support",
        (kind == "credit-note" ? -12.34m : 12.34m).ToString("0.00", CultureInfo.InvariantCulture))).ToArray();
    var total = (count * (kind == "credit-note" ? -12.34m : 12.34m)).ToString("0.00", CultureInfo.InvariantCulture);
    var file = $"{kind}-{count}.pdf";
    var watch = Stopwatch.StartNew();
    await using (var output = File.Create(Path.Combine(outputDirectory, file)))
        await template.RenderAsync(new InvoiceModel(kind.Replace('-', ' '), $"INV-{count:0000}", lines, total), output, cancellationToken: stop.Token);
    watch.Stop();
    results.Add(new { File = file, Rows = count, References = lines.Select(x => x.Reference), Total = total, Milliseconds = watch.Elapsed.TotalMilliseconds });
    Console.WriteLine($"{file}: {count} rows, {watch.Elapsed.TotalMilliseconds:F0} ms");
}
// Measured timings describe this run only; independent text/layout verification is a separate step.
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "manifest.json"), JsonSerializer.Serialize(new
{
    Platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Browser = FileVersionInfo.GetVersionInfo(browserPath).ProductVersion,
    OfflinePageNetworking = true,
    Results = results
}, new JsonSerializerOptions { WriteIndented = true }));

public sealed record InvoiceLine(string Reference, string Description, string Amount);
public sealed record InvoiceModel(string Kind, string Number, InvoiceLine[] Items, string Total);
