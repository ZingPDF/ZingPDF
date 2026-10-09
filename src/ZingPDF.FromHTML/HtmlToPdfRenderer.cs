using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ZingPDF.FromHTML;

/// <summary>
/// Reuses one owned Chromium process with a fresh isolated context per conversion.
/// Safe for concurrent calls up to the configured limit. Dispose after obtaining the required output.
/// </summary>
public sealed class HtmlToPdfRenderer : IAsyncDisposable
{
    private static readonly SemaphoreSlim Provisioning = new(1, 1);
    private readonly HtmlToPdfOptions _options;
    private readonly SemaphoreSlim _slots;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _sync = new();
    private Task<IBrowser>? _browserTask;
    private Task? _disposeTask;

    /// <summary>Creates a renderer without launching a browser. A preinstalled executable is required unless downloads are explicitly allowed. Configuration is copied.</summary>
    public HtmlToPdfRenderer(HtmlToPdfOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.PdfOptions);
        ArgumentNullException.ThrowIfNull(options.LaunchArguments);
        if (options.MaxConcurrency < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxConcurrency must be positive.");
        if (options.RenderTimeout != Timeout.InfiniteTimeSpan && (options.RenderTimeout < TimeSpan.FromMilliseconds(1) || options.RenderTimeout.TotalMilliseconds > int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(options), "RenderTimeout must be at least one millisecond, within Int32.MaxValue milliseconds, or Timeout.InfiniteTimeSpan.");
        if (options.BrowserStartupTimeout < TimeSpan.FromMilliseconds(1) || options.BrowserStartupTimeout.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(options), "BrowserStartupTimeout must be at least one millisecond and within Int32.MaxValue milliseconds.");
        if (string.IsNullOrWhiteSpace(options.BrowserExecutablePath) && !options.AllowBrowserDownload)
            throw new ArgumentException("Supply BrowserExecutablePath or explicitly allow browser downloads.", nameof(options));
        if (!string.IsNullOrWhiteSpace(options.BrowserExecutablePath) && !File.Exists(options.BrowserExecutablePath))
            throw new FileNotFoundException("The configured browser executable does not exist.", options.BrowserExecutablePath);

        _options = new HtmlToPdfOptions
        {
            BrowserExecutablePath = options.BrowserExecutablePath,
            AllowBrowserDownload = options.AllowBrowserDownload,
            MaxConcurrency = options.MaxConcurrency,
            RenderTimeout = options.RenderTimeout,
            BrowserStartupTimeout = options.BrowserStartupTimeout,
            LaunchArguments = options.LaunchArguments.ToArray(),
            MediaType = options.MediaType,
            Offline = options.Offline,
            PdfOptions = CopyPrintOptions(options.PdfOptions)
        };
        _slots = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
    }

    /// <summary>
    /// Converts trusted HTML to an owned readable, seekable PDF stream positioned at zero. The caller disposes the stream.
    /// Waits for document fonts. Cancellation closes the isolated job context; deadline expiry throws TimeoutException.
    /// Browser contexts do not replace OS process/network isolation for untrusted scripts.
    /// </summary>
    public Task<Stream> RenderAsync(string html, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(html);
        return RenderCoreAsync(page => page.SetContentAsync(html), cancellationToken);
    }

    /// <summary>Converts an absolute URL supported by Chromium with the navigation settings and renderer deadline. URL content must be trusted.</summary>
    public Task<Stream> RenderAsync(Uri uri, NavigationOptions? navigationOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            throw new ArgumentException("An absolute URL is required.", nameof(uri));
        var navigation = navigationOptions ?? new NavigationOptions();
        if (navigation.TimeoutExpiration < 0)
            throw new ArgumentOutOfRangeException(nameof(navigationOptions));
        var settings = new PuppeteerSharp.NavigationOptions
        {
            Timeout = navigation.TimeoutExpiration,
            WaitUntil = navigation.WaitUntilFlags is null ? [WaitUntilNavigation.Load] : navigation.WaitUntilFlags.ToWaitUntilNavigations()
        };
        return RenderCoreAsync(page => page.GoToAsync(uri.AbsoluteUri, settings), cancellationToken);
    }

    private async Task<Stream> RenderCoreAsync(Func<IPage, Task> load, CancellationToken cancellationToken)
    {
        lock (_sync)
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        deadline.CancelAfter(_options.RenderTimeout);
        var acquired = false;
        IBrowserContext? context = null;
        try
        {
            await _slots.WaitAsync(deadline.Token);
            acquired = true;
            Task<IBrowser> startup;
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
                startup = _browserTask ??= LaunchAsync();
            }
            // Startup remains renderer-owned if this caller stops waiting; disposal observes and closes it.
            var browser = await startup.WaitAsync(deadline.Token);
            // Never abandon context creation: its result must always be owned and closed.
            context = await browser.CreateBrowserContextAsync();
            deadline.Token.ThrowIfCancellationRequested();
            var page = await context.NewPageAsync();
            page.DefaultTimeout = _options.RenderTimeout == Timeout.InfiniteTimeSpan ? 30_000 : (int)_options.RenderTimeout.TotalMilliseconds;
            page.DefaultNavigationTimeout = page.DefaultTimeout;
            await page.SetOfflineModeAsync(_options.Offline).WaitAsync(deadline.Token);
            await page.EmulateMediaTypeAsync(_options.MediaType).WaitAsync(deadline.Token);
            await load(page).WaitAsync(deadline.Token);
            await page.EvaluateExpressionAsync("document.fonts.ready").WaitAsync(deadline.Token);
            var bytes = await page.PdfDataAsync(_options.PdfOptions).WaitAsync(deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            return new MemoryStream(bytes, writable: false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && !_lifetime.IsCancellationRequested)
        {
            throw new TimeoutException("HTML-to-PDF conversion exceeded its configured deadline.", exception);
        }
        finally
        {
            try
            {
                if (context is not null)
                    await context.CloseAsync();
            }
            finally
            {
                if (acquired)
                    _slots.Release();
            }
        }
    }

    private async Task<IBrowser> LaunchAsync()
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        startup.CancelAfter(_options.BrowserStartupTimeout);
        var startupWatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var executable = _options.BrowserExecutablePath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                await Provisioning.WaitAsync(startup.Token);
                try
                {
                    var fetcher = new BrowserFetcher(new BrowserFetcherOptions
                    {
                        CustomFileDownload = (address, file) => DownloadBrowserFileAsync(address, file, startup.Token)
                    });
                    var installed = await fetcher.DownloadAsync();
                    executable = installed.GetExecutablePath();
                }
                finally { Provisioning.Release(); }
            }
            startup.Token.ThrowIfCancellationRequested();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                ExecutablePath = executable,
                Headless = true,
                DumpIO = false,
                Args = _options.LaunchArguments,
                Timeout = Math.Max(1, (int)(_options.BrowserStartupTimeout - startupWatch.Elapsed).TotalMilliseconds),
                ProtocolTimeout = _options.RenderTimeout == Timeout.InfiniteTimeSpan ? 180_000 : (int)_options.RenderTimeout.TotalMilliseconds
            });
            if (startup.IsCancellationRequested)
            {
                await browser.DisposeAsync();
                startup.Token.ThrowIfCancellationRequested();
            }
            return browser;
        }
        catch (Exception exception) when (startup.IsCancellationRequested)
        {
            if (_lifetime.IsCancellationRequested)
                throw new OperationCanceledException("Browser startup was cancelled by renderer disposal.", exception, _lifetime.Token);
            throw new TimeoutException("Browser startup exceeded its configured deadline.", exception);
        }
    }

    internal static async Task DownloadBrowserFileAsync(string address, string file, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static PdfOptions CopyPrintOptions(PdfOptions print) => new()
    {
        Scale = print.Scale,
        DisplayHeaderFooter = print.DisplayHeaderFooter,
        HeaderTemplate = print.HeaderTemplate,
        FooterTemplate = print.FooterTemplate,
        PrintBackground = print.PrintBackground,
        Landscape = print.Landscape,
        PageRanges = print.PageRanges,
        Format = print.Format is null ? null : new PaperFormat(print.Format.Width, print.Format.Height),
        Width = print.Width,
        Height = print.Height,
        MarginOptions = print.MarginOptions is null ? null : new MarginOptions
        {
            Top = print.MarginOptions.Top, Bottom = print.MarginOptions.Bottom,
            Left = print.MarginOptions.Left, Right = print.MarginOptions.Right
        },
        PreferCSSPageSize = print.PreferCSSPageSize,
        OmitBackground = print.OmitBackground,
        Tagged = print.Tagged,
        Outline = print.Outline
    };

    /// <summary>Cancels queued/active conversions, closes their contexts and the owned browser. Repeated calls share one disposal task.</summary>
    public ValueTask DisposeAsync()
    {
        lock (_sync)
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    private async Task DisposeCoreAsync()
    {
        _lifetime.Cancel();
        for (var i = 0; i < _options.MaxConcurrency; i++)
            await _slots.WaitAsync();
        if (_browserTask is not null)
        {
            IBrowser browser;
            try { browser = await _browserTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (TimeoutException)
            {
                // Native installation/launch may outlive cancellation. Keep ownership of a late result
                // without making a cancelled one-shot conversion wait indefinitely for provisioning.
                _ = CloseLateBrowserAsync(_browserTask);
                return;
            }
            catch { return; } // Startup failed; the rendering caller receives its error.
            await browser.DisposeAsync();
        }
        // Keep synchronization objects alive so late calls observe a deterministic disposal error.
    }

    private static async Task CloseLateBrowserAsync(Task<IBrowser> startup)
    {
        try { await (await startup).DisposeAsync(); }
        catch { /* Observe failed startup/cleanup even after the renderer has been disposed. */ }
    }
}
