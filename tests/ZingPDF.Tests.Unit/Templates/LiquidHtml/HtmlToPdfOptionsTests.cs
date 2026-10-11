using FluentAssertions;
using Xunit;
using ZingPDF.FromHTML;

namespace ZingPDF.Tests.Unit.Templates.LiquidHtml;

public class HtmlToPdfOptionsTests
{
    [Fact]
    public void MissingBrowser_DoesNotImplicitlyEnableDownloads()
    {
        var construct = () => new HtmlToPdfRenderer(new HtmlToPdfOptions());
        construct.Should().Throw<ArgumentException>().WithMessage("*BrowserExecutablePath*");
    }

    [Fact]
    public void ExplicitMissingExecutable_ThrowsInsteadOfDownloading()
    {
        var construct = () => new HtmlToPdfRenderer(new HtmlToPdfOptions
        {
            BrowserExecutablePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing-browser"),
            AllowBrowserDownload = true
        });
        construct.Should().Throw<FileNotFoundException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidConcurrency_IsRejectedBeforeLaunching(int concurrency)
    {
        var construct = () => new HtmlToPdfRenderer(new HtmlToPdfOptions { AllowBrowserDownload = true, MaxConcurrency = concurrency });
        construct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void InvalidDeadline_IsRejectedBeforeLaunching(int milliseconds)
    {
        var construct = () => new HtmlToPdfRenderer(new HtmlToPdfOptions { AllowBrowserDownload = true, RenderTimeout = TimeSpan.FromMilliseconds(milliseconds) });
        construct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task PreCancelledConversion_DoesNotProvisionABrowser()
    {
        await using var renderer = new HtmlToPdfRenderer(new HtmlToPdfOptions { AllowBrowserDownload = true });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => renderer.RenderAsync("<p>Invoice</p>", cancellation.Token));
    }
}
