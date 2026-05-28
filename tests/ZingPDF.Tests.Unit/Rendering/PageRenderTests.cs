using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Rendering;
using ZingPDF.Syntax.CommonDataStructures;
using Xunit;
using PdfRectangle = ZingPDF.Syntax.CommonDataStructures.Rectangle;

namespace ZingPDF.Tests.Unit.Rendering;

[System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
[System.Runtime.Versioning.SupportedOSPlatform("ios13.6")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("maccatalyst13.5")]
[System.Runtime.Versioning.SupportedOSPlatform("macos")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class PageRenderTests
{
    [Fact]
    public async Task RenderAsync_CreatedPage_ReturnsPngWithExpectedDimensions()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(120, 80));
        var page = await pdf.GetPageAsync(1);

        var result = await page.RenderAsync(new PdfPageRenderOptions { Scale = 2d });

        result.PageNumber.Should().Be(1);
        result.PixelWidth.Should().Be(240);
        result.PixelHeight.Should().Be(160);
        result.Scale.Should().Be(2d);
        result.Geometry.DisplayWidth.Should().Be(120);
        result.Geometry.DisplayHeight.Should().Be(80);
        result.PngBytes.Length.Should().BeGreaterThan(0);

        using var image = Image.Load<Rgba32>(result.PngBytes.Span);
        image.Width.Should().Be(result.PixelWidth);
        image.Height.Should().Be(result.PixelHeight);
    }

    [Fact]
    public async Task RenderAsync_UnsavedPageEdits_IncludesCurrentPageContent()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(200, 100));
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        var page = await pdf.GetPageAsync(1);

        await page.AddTextAsync(
            "Preview",
            PdfRectangle.FromCoordinates(new Coordinate(20, 30), new Coordinate(180, 70)),
            font,
            28,
            RGBColour.Black);

        var result = await page.RenderAsync();

        using var image = Image.Load<Rgba32>(result.PngBytes.Span);
        CountNonWhitePixels(image).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderPageAsync_RendersRequestedPage()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(100, 100));
        await pdf.AppendPageAsync(options => options.MediaBox = PdfRectangle.FromDimensions(160, 90));

        var result = await pdf.RenderPageAsync(2);

        result.PageNumber.Should().Be(2);
        result.PixelWidth.Should().Be(160);
        result.PixelHeight.Should().Be(90);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task RenderAsync_InvalidScale_Throws(double scale)
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);

        var action = () => page.RenderAsync(new PdfPageRenderOptions { Scale = scale });

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task RenderAsync_CancelledToken_ThrowsBeforeRendering()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => page.RenderAsync(cancellationToken: cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private static int CountNonWhitePixels(Image<Rgba32> image)
    {
        var count = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250)
                    {
                        count++;
                    }
                }
            }
        });

        return count;
    }
}
