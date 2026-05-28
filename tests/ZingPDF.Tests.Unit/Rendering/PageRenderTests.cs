using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Rendering;
using ZingPDF.Syntax.CommonDataStructures;
using Xunit;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using ImageSharpImage = SixLabors.ImageSharp.Image;
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
    private static readonly RGBColour SnapshotBlue = new(0.12, 0.36, 0.86);
    private static readonly RGBColour SnapshotGreen = new(0.1, 0.64, 0.34);
    private static readonly RGBColour SnapshotRed = new(0.85, 0.16, 0.16);

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

        using var image = ImageSharpImage.Load<Rgba32>(result.PngBytes.Span);
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

        using var image = ImageSharpImage.Load<Rgba32>(result.PngBytes.Span);
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

    [Fact]
    public async Task RenderAsync_VectorSnapshot_MatchesApprovedSampleSignature()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(120, 80));
        var page = await pdf.GetPageAsync(1);
        await DrawSnapshotSampleAsync(page);

        var result = await page.RenderAsync();

        using var image = ImageSharpImage.Load<Rgba32>(result.PngBytes.Span);
        GetSnapshotSignature(image).Should().Be(
            "120x80|20,20=#1F5CDB|55,20=#1AA357|95,20=#D92929|20,60=#FFFFFF|55,60=#FFFFFF|95,60=#FFFFFF");
    }

    [Fact]
    public async Task RenderAsync_MapsPdfCoordinatesToExpectedDisplayPixels()
    {
        const double scale = 2d;
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(200, 100));
        var page = await pdf.GetPageAsync(1);
        var rectangle = PdfRectangle.FromCoordinates(new Coordinate(20, 30), new Coordinate(80, 60));

        await page.AddPathAsync(new DrawingPath(
            null,
            new FillOptions(RGBColour.Black),
            PathType.Linear,
            [
                rectangle.LowerLeft,
                new Coordinate(rectangle.UpperRight.X, rectangle.LowerLeft.Y),
                rectangle.UpperRight,
                new Coordinate(rectangle.LowerLeft.X, rectangle.UpperRight.Y),
                rectangle.LowerLeft
            ]));

        var result = await page.RenderAsync(new PdfPageRenderOptions { Scale = scale });
        var expectedTopLeft = result.Geometry.PageToDisplay(
            new Coordinate(rectangle.LowerLeft.X, rectangle.UpperRight.Y));
        var expectedBottomRight = result.Geometry.PageToDisplay(
            new Coordinate(rectangle.UpperRight.X, rectangle.LowerLeft.Y));

        using var image = ImageSharpImage.Load<Rgba32>(result.PngBytes.Span);

        var insideX = (int)Math.Round(((double)expectedTopLeft.X + 10) * scale);
        var insideY = (int)Math.Round(((double)expectedTopLeft.Y + 10) * scale);
        var outsideX = Math.Max(0, (int)Math.Round((double)expectedTopLeft.X * scale) - 8);
        var outsideY = Math.Max(0, (int)Math.Round((double)expectedTopLeft.Y * scale) - 8);

        image.Width.Should().Be(400);
        image.Height.Should().Be(200);
        expectedTopLeft.Should().BeEquivalentTo(new Coordinate(20, 40));
        expectedBottomRight.Should().BeEquivalentTo(new Coordinate(80, 70));
        IsNearBlack(image[insideX, insideY]).Should().BeTrue();
        IsNearWhite(image[outsideX, outsideY]).Should().BeTrue();
    }

    [Fact]
    public async Task RenderAsync_SnapshotChangesWhenPageContentChanges()
    {
        using var pdf = Pdf.Create(options => options.MediaBox = PdfRectangle.FromDimensions(120, 80));
        var page = await pdf.GetPageAsync(1);
        await DrawSnapshotSampleAsync(page);
        var firstRender = await page.RenderAsync();

        await page.AddPathAsync(new DrawingPath(
            null,
            new FillOptions(RGBColour.Black),
            PathType.Linear,
            [
                new Coordinate(46, 46),
                new Coordinate(74, 46),
                new Coordinate(74, 74),
                new Coordinate(46, 74),
                new Coordinate(46, 46)
            ]));

        var secondRender = await page.RenderAsync();

        using var firstImage = ImageSharpImage.Load<Rgba32>(firstRender.PngBytes.Span);
        using var secondImage = ImageSharpImage.Load<Rgba32>(secondRender.PngBytes.Span);
        GetDifferencePixelCount(firstImage, secondImage).Should().BeGreaterThan(500);
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

    private static async Task DrawSnapshotSampleAsync(Page page)
    {
        await DrawFilledRectangleAsync(page, 10, 50, 30, 20, SnapshotBlue);
        await DrawFilledRectangleAsync(page, 45, 50, 30, 20, SnapshotGreen);
        await DrawFilledRectangleAsync(page, 80, 50, 30, 20, SnapshotRed);
    }

    private static Task DrawFilledRectangleAsync(Page page, double left, double bottom, double width, double height, RGBColour fill)
        => page.AddPathAsync(new DrawingPath(
            null,
            new FillOptions(fill),
            PathType.Linear,
            [
                new Coordinate(left, bottom),
                new Coordinate(left + width, bottom),
                new Coordinate(left + width, bottom + height),
                new Coordinate(left, bottom + height),
                new Coordinate(left, bottom)
            ]));

    private static string GetSnapshotSignature(Image<Rgba32> image)
    {
        var points = new[]
        {
            new Coordinate(20, 20),
            new Coordinate(55, 20),
            new Coordinate(95, 20),
            new Coordinate(20, 60),
            new Coordinate(55, 60),
            new Coordinate(95, 60)
        };

        return $"{image.Width}x{image.Height}|{string.Join("|", points.Select(point => $"{point.X},{point.Y}={ToHex(image[(int)point.X, (int)point.Y])}"))}";
    }

    private static int GetDifferencePixelCount(Image<Rgba32> first, Image<Rgba32> second)
    {
        first.Width.Should().Be(second.Width);
        first.Height.Should().Be(second.Height);

        var count = 0;
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                if (ColorDistance(first[x, y], second[x, y]) > 10)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsNearBlack(Rgba32 pixel)
        => pixel.R < 20 && pixel.G < 20 && pixel.B < 20;

    private static bool IsNearWhite(Rgba32 pixel)
        => pixel.R > 245 && pixel.G > 245 && pixel.B > 245;

    private static string ToHex(Rgba32 pixel)
        => $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}";

    private static int ColorDistance(Rgba32 first, Rgba32 second)
        => Math.Abs(first.R - second.R)
            + Math.Abs(first.G - second.G)
            + Math.Abs(first.B - second.B)
            + Math.Abs(first.A - second.A);
}
