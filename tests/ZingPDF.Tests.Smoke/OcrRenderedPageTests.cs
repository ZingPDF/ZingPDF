using FakeItEasy;
using FluentAssertions;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using ZingPDF.Elements;
using ZingPDF.Graphics;
using ZingPDF.OCR;
using ZingPDF.Rendering;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Tests.Smoke.TestFiles;

namespace ZingPDF;

public sealed class OcrRenderedPageTests
{
    [Fact]
    public async Task RenderedPageMode_OcrsMixedTextAndImagePage()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        await page.AddWatermarkAsync("MIXED CONTENT HEADER");
        await page.AddImageAsync(Files.CatImage, Rectangle.FromDimensions(180, 180));
        var embeddedText = string.Join(" ", (await pdf.ExtractTextAsync(1)).Select(item => item.Text));
        embeddedText.Should().Contain("MIXED CONTENT HEADER");

        OcrInputImage? seenImage = null;
        var result = await pdf.ExtractTextWithOcrAsync(1, new DelegateOcrEngine(image =>
        {
            seenImage = image;
            return "recognized full page";
        }), new PdfOcrOptions { Mode = PdfOcrMode.RenderedPage, Dpi = 150 });

        result.Text.Should().Be("recognized full page");
        result.UsedEmbeddedText.Should().BeFalse();
        result.UsedOcr.Should().BeTrue();
        result.Source.Should().Be(OcrPageSource.RenderedPage);
        result.InputCoverage.Should().Be(OcrInputCoverage.RenderedPage);
        seenImage.Should().NotBeNull();
        seenImage!.PageNumber.Should().Be(1);
        seenImage.MimeType.Should().Be("image/png");
        seenImage.Data.Should().NotBeEmpty();
        result.SourceImageWidth.Should().Be(seenImage.Width);
        result.SourceImageHeight.Should().Be(seenImage.Height);
        seenImage.Width.Should().BeGreaterThan(1000);
        seenImage.Height.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task RenderedPageMode_IncludesImagesAtSeparatePagePositions()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        await page.AddImageAsync(Files.CatImage, Rectangle.FromCoordinates(
            new ZingPDF.Elements.Drawing.Coordinate(20, 20),
            new ZingPDF.Elements.Drawing.Coordinate(150, 150)));
        await page.AddImageAsync(Files.CatImage, Rectangle.FromCoordinates(
            new ZingPDF.Elements.Drawing.Coordinate(400, 600),
            new ZingPDF.Elements.Drawing.Coordinate(550, 750)));

        OcrInputImage? seenImage = null;
        _ = await pdf.ExtractTextWithOcrAsync(1, new DelegateOcrEngine(image =>
        {
            seenImage = image;
            return "whole page";
        }), new PdfOcrOptions { Mode = PdfOcrMode.RenderedPage, Dpi = 72 });

        seenImage.Should().NotBeNull();
        using var rendered = SixLabors.ImageSharp.Image.Load<Rgba32>(seenImage!.Data);
        CountNonWhite(rendered, 20, rendered.Height - 150, 150, rendered.Height - 20).Should().BeGreaterThan(100);
        CountNonWhite(rendered, 400, rendered.Height - 750, 550, rendered.Height - 600).Should().BeGreaterThan(100);
    }

    [Fact]
    public async Task RenderedPageMode_UsesRequestedPageNumberAndRejectsOversizedPagesBeforeRendering()
    {
        using var pdf = Pdf.Create();
        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(612, 792));
        var engine = new DelegateOcrEngine(image => $"page {image.PageNumber}");

        var result = await pdf.ExtractTextWithOcrAsync(2, engine, new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage,
            Dpi = 72
        });

        result.PageNumber.Should().Be(2);
        result.Text.Should().Be("page 2");
        result.InputCoverage.Should().Be(OcrInputCoverage.RenderedPage);

        var limited = () => pdf.ExtractTextWithOcrAsync(2, engine, new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage,
            Dpi = 300,
            MaxPixelCount = 1
        });
        await limited.Should().ThrowAsync<InvalidOperationException>().WithMessage("*exceeds the rendered OCR image limit*");
    }

    [Fact]
    public async Task RenderedPageMode_RejectsUnboundedOptions()
    {
        using var pdf = Pdf.Create();
        var engine = new DelegateOcrEngine(_ => "unused");

        var dpiAction = () => pdf.ExtractTextWithOcrAsync(1, engine, new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage,
            Dpi = 601
        });
        await dpiAction.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("Dpi");

        var pixelAction = () => pdf.ExtractTextWithOcrAsync(1, engine, new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage,
            MaxPixelCount = 50_000_001
        });
        await pixelAction.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("MaxPixelCount");
    }

    [Fact]
    public async Task RenderedPageMode_PropagatesRendererFailure()
    {
        using var source = Pdf.Create();
        var page = await source.GetPageAsync(1);
        var pdf = A.Fake<IPdf>();
        A.CallTo(() => pdf.GetPageAsync(1)).Returns(Task.FromResult(page));
        A.CallTo(() => pdf.RenderPageAsync(1, A<PdfPageRenderOptions>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("renderer failed"));

        var action = () => pdf.ExtractTextWithOcrAsync(1, new DelegateOcrEngine(_ => "unused"), new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage
        });

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("renderer failed");
    }

    [Fact]
    public async Task RenderedPageMode_PropagatesEngineFailureAndCancellation()
    {
        using var pdf = Pdf.Create();
        var failing = new DelegateOcrEngine(_ => throw new InvalidOperationException("engine failed"));
        var options = new PdfOcrOptions { Mode = PdfOcrMode.RenderedPage, Dpi = 72 };

        var engineAction = () => pdf.ExtractTextWithOcrAsync(1, failing, options);
        await engineAction.Should().ThrowAsync<InvalidOperationException>().WithMessage("engine failed");

        using var cancellation = new CancellationTokenSource();
        var recognitionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishRecognition = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ignoresCancellation = new BlockingOcrEngine(recognitionStarted, finishRecognition.Task);
        var canceledOperation = pdf.ExtractTextWithOcrAsync(1, ignoresCancellation, options, cancellation.Token);
        await recognitionStarted.Task;
        cancellation.Cancel();
        finishRecognition.SetResult("recognizer ignored cancellation");
        var canceledAction = () => canceledOperation;
        await canceledAction.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RenderedPageMode_RejectsOversizedRendererOutputBeforeCallingEngine()
    {
        using var source = Pdf.Create();
        var page = await source.GetPageAsync(1);
        var geometry = await page.GetGeometryAsync();
        var pdf = A.Fake<IPdf>();
        A.CallTo(() => pdf.GetPageAsync(1)).Returns(Task.FromResult(page));
        A.CallTo(() => pdf.RenderPageAsync(1, A<PdfPageRenderOptions>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new PdfPageRenderResult
            {
                PageNumber = 1,
                PixelWidth = 1200,
                PixelHeight = 1000,
                Scale = 1,
                Geometry = geometry,
                PngBytes = new byte[] { 1 }
            }));
        var engineCalled = false;
        var engine = new DelegateOcrEngine(_ =>
        {
            engineCalled = true;
            return "should not run";
        });

        var action = () => pdf.ExtractTextWithOcrAsync(1, engine, new PdfOcrOptions
        {
            Mode = PdfOcrMode.RenderedPage,
            Dpi = 72,
            MaxPixelCount = 1_000_000
        });

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*renderer output exceeds the OCR image limit*");
        engineCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ImageCandidateMode_ReportsPartialCoverageAndEmbeddedTextReportsNoOcrCoverage()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        await page.AddImageAsync(Files.CatImage, Rectangle.FromDimensions(180, 180));

        var candidate = await pdf.ExtractTextWithOcrAsync(1, new DelegateOcrEngine(_ => "candidate"), new PdfOcrOptions
        {
            PreferEmbeddedText = false
        });
        candidate.Source.Should().Be(OcrPageSource.ImageCandidate);
        candidate.InputCoverage.Should().Be(OcrInputCoverage.ImageCandidate);

        await page.AddWatermarkAsync("EMBEDDED HEADER");
        var embedded = await pdf.ExtractTextWithOcrAsync(1, new DelegateOcrEngine(_ => "unused"));
        embedded.Source.Should().Be(OcrPageSource.EmbeddedText);
        embedded.InputCoverage.Should().Be(OcrInputCoverage.None);
    }

    private sealed class DelegateOcrEngine(Func<OcrInputImage, string> handler) : IOcrEngine
    {
        public Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(handler(image));
        }
    }

    private sealed class BlockingOcrEngine(TaskCompletionSource started, Task<string> completion) : IOcrEngine
    {
        public async Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default)
        {
            started.SetResult();
            return await completion;
        }
    }

    private static int CountNonWhite(SixLabors.ImageSharp.Image<Rgba32> image, int left, int top, int right, int bottom)
    {
        var count = 0;
        for (var y = Math.Max(0, top); y < Math.Min(image.Height, bottom); y++)
        {
            for (var x = Math.Max(0, left); x < Math.Min(image.Width, right); x++)
            {
                var pixel = image[x, y];
                if (pixel.R < 245 || pixel.G < 245 || pixel.B < 245)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
