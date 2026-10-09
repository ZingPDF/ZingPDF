using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ZingPDF.FromHTML;

/// <summary>Browser deployment and print settings for an owned HTML-to-PDF renderer.</summary>
public sealed class HtmlToPdfOptions
{
    /// <summary>Path to a preinstalled Chromium-compatible executable. No download is attempted when supplied.</summary>
    public string? BrowserExecutablePath { get; init; }
    /// <summary>Allows browser download when no executable path is supplied. Defaults to false.</summary>
    public bool AllowBrowserDownload { get; init; }
    /// <summary>Maximum simultaneous isolated browser contexts. Additional jobs wait with cancellation and timeout support.</summary>
    public int MaxConcurrency { get; init; } = 4;
    /// <summary>Conversion deadline including queueing, startup, navigation, fonts and printing. Defaults to 30 seconds. Timeout.InfiniteTimeSpan disables the overall deadline.</summary>
    public TimeSpan RenderTimeout { get; init; } = TimeSpan.FromSeconds(30);
    /// <summary>Positive deadline for browser provisioning and launch. Defaults to 30 seconds, independently of the conversion deadline.</summary>
    public TimeSpan BrowserStartupTimeout { get; init; } = TimeSpan.FromSeconds(30);
    /// <summary>Additional Chromium arguments. The renderer does not disable the browser sandbox by default.</summary>
    public string[] LaunchArguments { get; init; } = [];
    /// <summary>CSS media used for conversion. Defaults to print; select screen to match the original static converter.</summary>
    public MediaType MediaType { get; init; } = MediaType.Print;
    /// <summary>PDF paper, margins, headers, backgrounds and page-range settings. Copied at renderer construction.</summary>
    public PdfOptions PdfOptions { get; init; } = new() { PrintBackground = true, PreferCSSPageSize = true };
    /// <summary>Emulates offline page networking. Embed fonts/images as data URLs. This is not an OS-level network sandbox.</summary>
    public bool Offline { get; init; }
}
