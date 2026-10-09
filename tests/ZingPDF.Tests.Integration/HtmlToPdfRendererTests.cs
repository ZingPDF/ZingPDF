using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Xunit;
using ZingPDF.FromHTML;
using ZingPDF.Templates;
using ZingPDF.Templates.LiquidHtml;
using NavigationOptions = ZingPDF.FromHTML.NavigationOptions;

namespace ZingPDF.Tests.Integration;

/// <summary>Acceptance checks that generate and read real Chromium PDFs.</summary>
public class HtmlToPdfRendererTests
{
    [BrowserTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(40)]
    [InlineData(500)]
    public async Task LiquidInvoice_PaginatesEveryRowAndRepeatsHeaders(int rowCount)
    {
        await using var renderer = CreateRenderer();
        var source = PdfTemplateSource.FromString("""
            <!doctype html><html><head><style>
            @page { size: A4; margin: 15mm; }
            body { font-family: Arial, sans-serif; font-size: 12px; }
            table { width: 100%; border-collapse: collapse; }
            thead { display: table-header-group; }
            tr { break-inside: avoid; }
            td, th { height: 27px; text-align: left; }
            </style></head><body><h1>Invoice acceptance corpus</h1>
            <table><thead><tr><th>Item reference</th><th>Amount AUD</th></tr></thead><tbody>
            {% for row in Rows %}<tr><td>{{ row.Reference }}</td><td>{{ row.Amount }}</td></tr>{% endfor %}
            </tbody></table><p>FINAL-TOTAL {{ Total }}</p></body></html>
            """);
        var template = LiquidHtmlPdfTemplate.FromSource(source, new RendererHtmlToPdfConverter(renderer));
        var rows = Enumerable.Range(1, rowCount).Select(i => new InvoiceRow($"ITEM-{i:0000}-END", $"{i}.25")).ToArray();
        using var output = new MemoryStream();
        await template.RenderAsync(new InvoiceModel(rows, $"{rowCount}.00"), output);
        output.Position = 0;
        using var pdf = Pdf.Load(output);
        var pageCount = await pdf.GetPageCountAsync();
        pageCount.Should().BeGreaterThan(0);
        if (rowCount >= 40) pageCount.Should().BeGreaterThan(1);
        var text = await TextAsync(pdf);
        foreach (var row in rows)
        {
            text.Split(row.Reference).Length.Should().Be(2, $"{row.Reference} must occur exactly once");
            text.Should().Contain(row.Amount);
        }
        text.Should().Contain("FINAL-TOTAL").And.Contain($"{rowCount}.00");
        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            var page = await pdf.GetPageAsync(pageNumber);
            var box = await page.Dictionary.MediaBox.GetAsync();
            box.Width.Value.Should().BeApproximately(595.28, 1);
            box.Height.Value.Should().BeApproximately(841.89, 1);
            var pageText = string.Join(" ", (await pdf.ExtractTextAsync(pageNumber)).Select(x => x.Text));
            if (pageText.Contains("ITEM-")) pageText.Should().Contain("Item reference").And.Contain("Amount AUD");
        }
    }

    [BrowserFact]
    public async Task MediaTypeAndPdfOptions_ControlPrintedContentAndPageSize()
    {
        const string html = "<style>@page{size:A4}@media print{.screen{display:none}}@media screen{.print{display:none}}</style><p class='print'>PRINT-MARKER</p><p class='screen'>SCREEN-MARKER</p>";
        await using var print = CreateRenderer();
        using var printed = await print.RenderAsync(html);
        using var printedPdf = Pdf.Load(printed);
        (await TextAsync(printedPdf)).Should().Contain("PRINT-MARKER").And.NotContain("SCREEN-MARKER");
        var options = new PdfOptions { Width = "3in", Height = "4in", PreferCSSPageSize = false };
        await using var screen = CreateRenderer(media: MediaType.Screen, pdfOptions: options);
        options.Width = "8in";
        options.Height = "12in";
        options.PreferCSSPageSize = true;
        using var screened = await screen.RenderAsync(html);
        using var screenedPdf = Pdf.Load(screened);
        (await TextAsync(screenedPdf)).Should().Contain("SCREEN-MARKER").And.NotContain("PRINT-MARKER");
        var box = await (await screenedPdf.GetPageAsync(1)).Dictionary.MediaBox.GetAsync();
        box.Width.Value.Should().BeApproximately(216, 1);
        box.Height.Value.Should().BeApproximately(288, 1);
    }

    [BrowserFact]
    public async Task ConcurrentRenders_IsolateLocalStorageAndCookies()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(concurrency: 2);
        var renders = Enumerable.Range(0, 6).Select(async i =>
        {
            using var output = await renderer.RenderAsync(new Uri(server.BaseUri, $"isolation?run={i}"));
            using var pdf = Pdf.Load(output);
            (await TextAsync(pdf)).Should().Contain("CLEAN-CONTEXT").And.NotContain("LEAKED-CONTEXT");
        });
        await Task.WhenAll(renders);
    }

    [BrowserFact]
    public async Task UriNavigationOptions_HonorDomContentLoadedAndNetworkIdleWaits()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer();
        using var output = await renderer.RenderAsync(new Uri(server.BaseUri, "background-fetch"),
            new NavigationOptions { WaitUntilFlags = WaitUntil.DOMContentLoaded });
        using var pdf = Pdf.Load(output);
        (await TextAsync(pdf)).Should().Contain("DOM-READY");
        var failure = await Assert.ThrowsAnyAsync<NavigationException>(async () =>
            await renderer.RenderAsync(new Uri(server.BaseUri, "background-fetch"),
                new NavigationOptions { WaitUntilFlags = WaitUntil.Networkidle0, TimeoutExpiration = 500 })
                .WaitAsync(TimeSpan.FromSeconds(5)));
        failure.Message.Should().Contain("500", "the configured navigation timeout must govern the background request wait");
    }

    [BrowserFact]
    public async Task FontReadiness_WaitsForDelayedFontAfterDomContentLoaded()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer();
        using var output = await renderer.RenderAsync(new Uri(server.BaseUri, "delayed-font"),
            new NavigationOptions { WaitUntilFlags = WaitUntil.DOMContentLoaded });
        using var pdf = Pdf.Load(output);
        var glyphText = string.Concat((await TextAsync(pdf)).Where(c => !char.IsWhiteSpace(c)));
        glyphText.Should().Contain("FONT-READY").And.Contain("Embeddedinvoicefont");
    }

    [BrowserFact]
    public async Task OfflineMode_RendersEmbeddedFontWithoutRequestingExternalAssets()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(offline: true);
        var font = Convert.ToBase64String(await File.ReadAllBytesAsync(FontFixturePath()));
        using var output = await renderer.RenderAsync($"<style>@font-face{{font-family:Invoice;src:url(data:font/ttf;base64,{font})}}body{{font-family:Invoice}}</style><p>OFFLINE-INVOICE</p><img src='{new Uri(server.BaseUri, "external-image")}'>");
        using var pdf = Pdf.Load(output);
        (await TextAsync(pdf)).Should().Contain("OFFLINE-INVOICE");
        server.RequestCount.Should().Be(0, "offline mode must block the external image request");
    }

    [BrowserFact]
    public async Task Cancellation_InterruptsRunningNavigationAndReleasesItsSlot()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(concurrency: 1);
        using var warmup = await renderer.RenderAsync("warmup");
        using var cancellation = new CancellationTokenSource();
        var blocked = renderer.RenderAsync(new Uri(server.BaseUri, "stall"), cancellationToken: cancellation.Token);
        await server.StallRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await blocked.WaitAsync(TimeSpan.FromSeconds(5)));
        using var recovered = await renderer.RenderAsync("<p>RECOVERED</p>");
        using var pdf = Pdf.Load(recovered);
        (await TextAsync(pdf)).Should().Contain("RECOVERED");
    }

    [BrowserFact]
    public async Task Cancellation_InterruptsQueuedRenderWithoutDisturbingTheActiveRender()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(concurrency: 1);
        using var warmup = await renderer.RenderAsync("warmup");
        using var activeCancellation = new CancellationTokenSource();
        var active = renderer.RenderAsync(new Uri(server.BaseUri, "stall"), cancellationToken: activeCancellation.Token);
        await server.StallRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using var queuedCancellation = new CancellationTokenSource();
        var queued = renderer.RenderAsync("queued", queuedCancellation.Token);
        queuedCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queued.WaitAsync(TimeSpan.FromSeconds(5)));
        active.IsCompleted.Should().BeFalse();
        activeCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await active.WaitAsync(TimeSpan.FromSeconds(5)));
        using var recovered = await renderer.RenderAsync("recovered");
    }

    [BrowserFact]
    public async Task Timeout_ReleasesTheRenderSlotForTheNextDocument()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(concurrency: 1, timeout: TimeSpan.FromSeconds(4));
        using var warmup = await renderer.RenderAsync("warmup");
        await Assert.ThrowsAsync<TimeoutException>(async () => await renderer.RenderAsync(new Uri(server.BaseUri, "stall")));
        using var recovered = await renderer.RenderAsync("<p>AFTER-TIMEOUT</p>");
        using var pdf = Pdf.Load(recovered);
        (await TextAsync(pdf)).Should().Contain("AFTER-TIMEOUT");
    }

    [BrowserFact]
    public async Task DataUri_RendersAnAbsoluteChromiumUrl()
    {
        await using var renderer = CreateRenderer();
        var uri = new Uri("data:text/html," + Uri.EscapeDataString("<p>DATA-URI-INVOICE</p>"));
        using var output = await renderer.RenderAsync(uri);
        using var pdf = Pdf.Load(output);
        (await TextAsync(pdf)).Should().Contain("DATA-URI-INVOICE");
    }

    [Fact]
    public async Task BrowserDownload_CancelsWhileWaitingForHeadersAndWhileStreamingBody()
    {
        foreach (var endpoint in new[] { "stall", "stall-body" })
        {
            await using var server = new LocalServer();
            using var cancellation = new CancellationTokenSource();
            var file = System.IO.Path.GetTempFileName();
            try
            {
                var download = HtmlToPdfRenderer.DownloadBrowserFileAsync(new Uri(server.BaseUri, endpoint).AbsoluteUri, file, cancellation.Token);
                await server.StallRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
                if (endpoint == "stall-body")
                {
                    using var progressDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    while (new FileInfo(file).Length == 0)
                        await Task.Delay(10, progressDeadline.Token);
                }
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await download.WaitAsync(TimeSpan.FromSeconds(5)));
                // Cancellation must close the downloader's output file as well as the HTTP request.
                using var exclusive = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            finally { File.Delete(file); }
        }
    }

    [BrowserFact]
    public async Task Dispose_CancelsActiveAndQueuedRendersAndRejectsNewWork()
    {
        await using var server = new LocalServer();
        await using var renderer = CreateRenderer(concurrency: 1);
        using var warmup = await renderer.RenderAsync("warmup");
        var active = renderer.RenderAsync(new Uri(server.BaseUri, "stall"));
        await server.StallRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var queued = renderer.RenderAsync("queued");
        var disposal = renderer.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await active.WaitAsync(TimeSpan.FromSeconds(5)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queued.WaitAsync(TimeSpan.FromSeconds(5)));
        await disposal.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => renderer.RenderAsync("after disposal"));
    }

    [BrowserFact]
    public async Task Dispose_IsIdempotentAndRejectsFurtherRenders()
    {
        var renderer = CreateRenderer();
        using var output = await renderer.RenderAsync("before disposal");
        await renderer.DisposeAsync();
        await renderer.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => renderer.RenderAsync("after disposal"));
    }

    private static HtmlToPdfRenderer CreateRenderer(int concurrency = 4, TimeSpan? timeout = null,
        MediaType media = MediaType.Print, PdfOptions? pdfOptions = null, bool offline = false) => new(new HtmlToPdfOptions
        {
            BrowserExecutablePath = BrowserLocation.Path,
            AllowBrowserDownload = false,
            MaxConcurrency = concurrency,
            RenderTimeout = timeout ?? TimeSpan.FromSeconds(30),
            MediaType = media,
            Offline = offline,
            PdfOptions = pdfOptions ?? new PdfOptions { PrintBackground = true, PreferCSSPageSize = true }
        });

    private static async Task<string> TextAsync(Pdf pdf) => string.Join(" ", await Task.Run(async () =>
        (await pdf.ExtractTextAsync()).Select(x => x.Text).ToArray()).WaitAsync(TimeSpan.FromSeconds(10)));

    private static string FontFixturePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = System.IO.Path.Combine(directory.FullName, "tests", "TestFiles", "font", "NotoSans-Regular.ttf");
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Cannot locate NotoSans font fixture.");
    }

    public sealed record InvoiceRow(string Reference, string Amount);
    public sealed record InvoiceModel(InvoiceRow[] Rows, string Total);

    private sealed class LocalServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private int _requestCount;
        public Uri BaseUri { get; }
        public int RequestCount => Volatile.Read(ref _requestCount);
        public TaskCompletionSource StallRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LocalServer()
        {
            var portProbe = new TcpListener(IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();
            BaseUri = new Uri($"http://127.0.0.1:{port}/");
            _listener.Prefixes.Add(BaseUri.AbsoluteUri);
            _listener.Start();
            _loop = ServeAsync();
        }

        private async Task ServeAsync()
        {
            try
            {
                while (!_stop.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    Interlocked.Increment(ref _requestCount);
                    if (context.Request.Url!.AbsolutePath == "/stall")
                    {
                        StallRequested.TrySetResult();
                        continue;
                    }
                    if (context.Request.Url.AbsolutePath == "/stall-body")
                    {
                        context.Response.ContentLength64 = 1024 * 1024;
                        await context.Response.OutputStream.WriteAsync(new byte[8192]);
                        await context.Response.OutputStream.FlushAsync();
                        StallRequested.TrySetResult();
                        continue;
                    }
                    if (context.Request.Url.AbsolutePath == "/font.ttf")
                    {
                        await Task.Delay(500, _stop.Token);
                        var fontBytes = await File.ReadAllBytesAsync(FontFixturePath(), _stop.Token);
                        context.Response.ContentType = "font/ttf";
                        context.Response.ContentLength64 = fontBytes.Length;
                        await context.Response.OutputStream.WriteAsync(fontBytes);
                        context.Response.Close();
                        continue;
                    }
                    var html = context.Request.Url.AbsolutePath == "/background-fetch"
                        ? "<p>DOM-READY</p><script>fetch('/stall');</script>"
                        : context.Request.Url.AbsolutePath == "/delayed-font"
                            ? "<style>@font-face{font-family:Invoice;src:url('/font.ttf')}body{font-family:Invoice}</style><p>Embedded invoice font</p><script>document.fonts.ready.then(()=>document.body.append('FONT-READY'))</script>"
                            : "<script>document.write(localStorage.getItem('previous')||document.cookie?'LEAKED-CONTEXT':'CLEAN-CONTEXT');localStorage.setItem('previous','yes');document.cookie='previous=yes;path=/';</script>";
                    var bytes = Encoding.UTF8.GetBytes(html);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (_stop.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (_stop.IsCancellationRequested) { }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Close();
            await _loop;
            _stop.Dispose();
        }
    }
}

internal static class BrowserLocation
{
    public static string? Path { get; } = Locate();
    private static string? Locate()
    {
        var configured = Environment.GetEnvironmentVariable("ZINGPDF_BROWSER_PATH");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        return new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            "/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/chromium-browser",
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
        }.FirstOrDefault(File.Exists);
    }
}

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (BrowserLocation.Path is null) Skip = "Real browser required. Set ZINGPDF_BROWSER_PATH to a provisioned Chromium executable.";
    }
}

public sealed class BrowserTheoryAttribute : TheoryAttribute
{
    public BrowserTheoryAttribute()
    {
        if (BrowserLocation.Path is null) Skip = "Real browser required. Set ZINGPDF_BROWSER_PATH to a provisioned Chromium executable.";
    }
}
