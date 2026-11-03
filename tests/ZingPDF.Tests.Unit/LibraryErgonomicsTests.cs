using System.Text;
using FluentAssertions;
using Xunit;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Graphics;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;
using ZingPDF.Syntax.Encryption;

namespace ZingPDF.Tests.Unit;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macos")]
public class LibraryErgonomicsTests
{
    private static string FontPath => System.IO.Path.Combine(AppContext.BaseDirectory, "TestFonts", "NotoSans-Regular.ttf");

    [Fact]
    public async Task UnicodeFont_NormalPageApis_ExpandCoverageAcrossPagesAndPreserveAliases()
    {
        using var pdf = Pdf.Create();
        var font = await pdf.RegisterUnicodeTrueTypeFontAsync(FontPath);
        font.GetMissingCharacters("Latin \u03A9 \u0416").Should().BeEmpty();
        font.GetMissingCharacters("\u4E00\u4E00").Should().Be("\u4E00");
        var page = await pdf.GetPageAsync(1);
        await page.AddTextAsync("Latin \u03A9 \u00A0", new Coordinate(30, 200), font, 14, RGBColour.Black);
        var firstRender = await pdf.RenderPageAsync(1);
        var next = await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(595, 842));
        await next.AddTextAsync("Later \u0416", Rectangle.FromDimensions(400, 200), font, 14, RGBColour.Black);
        var source = (await pdf.CreateTextEditor().GetLinesAsync(1)).Single();
        ((double)source.Bounds.Width).Should().BeApproximately(font.MeasureText(source.Text, 14), .02);
        Func<Task> reject = () => next.AddTextAsync("\u4E00", new Coordinate(30, 200), font, 14, RGBColour.Black);
        await reject.Should().ThrowAsync<ArgumentException>();
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        (await saved.CreateTextEditor().GetLinesAsync(1)).Single().Text.Should().Be("Latin \u03A9 \u00A0");
        (await saved.CreateTextEditor().GetLinesAsync(2)).Single().Text.Should().Be("Later \u0416");
        (await saved.RenderPageAsync(1)).PngBytes.ToArray().Should().Equal(firstRender.PngBytes.ToArray());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TypedFontStreamProperties_ReadCreatedLoadedAndEncryptedFonts(bool encrypted)
    {
        var bytes = await File.ReadAllBytesAsync(FontPath);
        using var pdf = Pdf.Create();
        using var input = new MemoryStream(bytes);
        var font = await pdf.RegisterTrueTypeFontAsync(input);
        await (await pdf.GetPageAsync(1)).AddTextAsync("Font", new Coordinate(20, 100), font, 12, RGBColour.Black);
        (await pdf.IsEncryptedAsync()).Should().BeFalse();
        if (encrypted) { await pdf.EncryptAsync("font-test-password", algorithm: PdfEncryptionAlgorithm.Aes128); (await pdf.IsEncryptedAsync()).Should().BeTrue(); }
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        (await saved.IsEncryptedAsync()).Should().Be(encrypted);
        if (encrypted) await saved.AuthenticateAsync("font-test-password");
        var resources = new ResourceDictionary((await (await saved.GetPageAsync(1)).Dictionary.Resources.GetAsync())!);
        var fonts = (await resources.Font.GetAsync())!;
        var dictionary = await fonts.GetRequiredProperty<FontDictionary>(font.ResourceName.Value).GetAsync();
        var descriptor = (await dictionary.FontDescriptor.GetAsync())!;
        var optional = await descriptor.FontFile2.GetAsync();
        var required = await descriptor.GetRequiredProperty<StreamObject<IStreamDictionary>>("FontFile2").GetAsync();
        optional.Should().BeSameAs(required);
        using var decoded = await optional!.GetDecompressedDataAsync(); using var result = new MemoryStream(); await decoded.CopyToAsync(result);
        result.ToArray().Should().Equal(bytes);
        using var second = await required.GetDecompressedDataAsync(); second.Length.Should().Be(bytes.Length);
    }

    [Fact]
    public async Task TextPlacement_ExplicitLinesRotationAndInheritedGraphicsState_AreHandledByLibrary()
    {
        using var pdf = Pdf.Create(o => o.MediaBox = Rectangle.FromDimensions(300, 300));
        var page = await pdf.GetPageAsync(1);
        var stream = new StreamObject<StreamDictionary>(new MemoryStream(Encoding.ASCII.GetBytes("2 0 0 2 100 100 cm 0 0 1 1 re W n")), new StreamDictionary(pdf, ObjectContext.UserCreated));
        page.Dictionary.Set("Contents", (await pdf.Objects.AddAsync(stream)).Reference);
        var font = await pdf.RegisterStandardFontAsync("Helvetica");
        await page.AddTextAsync("One\r\nTwo\rThree\nFour", new Coordinate(40, 200), font, 12, RGBColour.Black,
            new TextPlacementOptions { RotationDegrees = 90, LineHeight = 20 });
        var lines = await pdf.CreateTextEditor().GetLinesAsync(1);
        lines.Select(l => l.Text).Should().Equal("One", "Two", "Three", "Four");
        lines.Select(l => l.BaselineOrigin.X).Should().Equal(40, 60, 80, 100);
        lines.Should().OnlyContain(l => Math.Abs(l.BaselineOrigin.Y - 200) < .01);
        var render = await pdf.RenderPageAsync(1);
        using var pixels = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(render.PngBytes.Span);
        var dark = 0;
        for (var y = 0; y < pixels.Height; y++) for (var x = 0; x < pixels.Width; x++) if (pixels[x,y].R < 100) dark++;
        dark.Should().BeGreaterThan(50, "original clipping and transformation must not suppress the added text");
    }

    [Fact]
    public async Task FontResourceCollision_DoesNotChangeOriginalTextOrAnotherPage()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var original = await pdf.RegisterStandardFontAsync("Helvetica", "F1");
        await page.AddTextAsync("Original", new Coordinate(20, 100), original, 12, RGBColour.Black);
        var second = await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(595, 842));
        second.Dictionary.Set("Resources", await page.Dictionary.Resources.GetRawValueAsync());
        second.Dictionary.Set("Contents", await page.Dictionary.Contents.GetRawValueAsync());
        var other = await pdf.RegisterUnicodeTrueTypeFontAsync(FontPath, "F1");
        await page.AddTextAsync("New \u03A9", new Coordinate(20, 180), other, 12, RGBColour.Black);
        var lines = await pdf.CreateTextEditor().GetLinesAsync(1);
        lines.Single(l => l.Text == "Original").FontName.Should().Be("Helvetica");
        lines.Single(l => l.Text == "New \u03A9").IsFontEmbedded.Should().BeTrue();
        (await pdf.CreateTextEditor().GetLinesAsync(2)).Should().ContainSingle(l => l.Text == "Original");
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        (await saved.CreateTextEditor().GetLinesAsync(1)).Select(l => l.Text).Should().Equal("Original", "New \u03A9");
        (await saved.CreateTextEditor().GetLinesAsync(2)).Should().ContainSingle(l => l.Text == "Original");
    }
    [Fact]
    public async Task UnicodeFluentAuthoring_UsesTheSameFontRegistrationAndCoverage()
    {
        using var output = new MemoryStream();
        await Pdf.New().Page(page => page.Text(t => t.Value("Fluent \u03A9 \u0416").WithUnicodeTrueTypeFont(FontPath).At(30, 200).FontSize(14))).SaveAsync(output);
        output.Position = 0; using var saved = Pdf.Load(output);
        (await saved.CreateTextEditor().GetLinesAsync(1)).Should().ContainSingle(l => l.Text == "Fluent \u03A9 \u0416");
    }

    [Fact]
    public async Task FontFromAnotherDocument_IsRejectedWithoutChangingPageContent()
    {
        using var first = Pdf.Create(); using var second = Pdf.Create();
        var font = await first.RegisterStandardFontAsync("Helvetica");
        var page = await second.GetPageAsync(1);
        var contents = await page.Dictionary.Contents.GetRawValueAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => page.AddTextAsync("Wrong document", Coordinate.Zero, font, 12, RGBColour.Black));
        (await page.Dictionary.Contents.GetRawValueAsync()).Should().Be(contents);
    }
    [Fact]
    public async Task ExistingTextReplacement_AcceptsRegisteredFont_AndChecksWithoutAddingMappings()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var original = await pdf.RegisterStandardFontAsync("Helvetica");
        await page.AddTextAsync("Original", new Coordinate(30, 200), original, 14, RGBColour.Black);
        var replacement = await pdf.RegisterUnicodeTrueTypeFontAsync(FontPath);
        var editor = pdf.CreateTextEditor(); var line = (await editor.GetLinesAsync(1)).Single();
        var options = new ZingPDF.Text.Editing.PdfTextReplacementOptions { Font = replacement };
        var fontObject = await pdf.Objects.GetAsync<FontDictionary>(replacement.FontReference);
        var unicodeStream = (await fontObject.ToUnicode.GetAsync())!;
        var length = unicodeStream.Data.Length;
        (await editor.CheckReplacementAsync(line, "Changed \u03A9", options)).CanApply.Should().BeTrue();
        unicodeStream.Data.Length.Should().Be(length, "checking must not mutate the registered font's mappings");
        await editor.ReplaceLineAsync(line, "Changed \u03A9", options);
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        (await saved.CreateTextEditor().GetLinesAsync(1)).Should().ContainSingle(l => l.Text == "Changed \u03A9");
    }
    [Fact]
    public async Task UnicodeBoxLayout_WrapAndShrink_HandleExplicitLineBreaks()
    {
        using var pdf = Pdf.Create(); var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterUnicodeTrueTypeFontAsync(FontPath);
        await page.AddTextAsync("First \u03A9\nSecond \u0416", Rectangle.FromDimensions(90, 40), font, 20, RGBColour.Black,
            new TextLayoutOptions { Wrap = true, Overflow = TextOverflowMode.ShrinkToFit });
        var lines = await pdf.CreateTextEditor().GetLinesAsync(1);
        lines.Should().HaveCount(2); lines[0].Text.Should().Be("First \u03A9"); lines[1].Text.Should().Be("Second \u0416");
        lines.Should().OnlyContain(l => l.FontSize > 0 && l.FontSize <= 20);
    }
    [Fact]
    public async Task RetainedPageHandle_RendersItsOwnContentAfterMove()
    {
        using var pdf = Pdf.Create(); var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync("Helvetica");
        await page.AddTextAsync("First page", new Coordinate(30, 200), font, 14, RGBColour.Black);
        var other = await pdf.AppendPageAsync(o => o.MediaBox = Rectangle.FromDimensions(595, 842));
        await other.AddTextAsync("Different", new Coordinate(30, 200), font, 14, RGBColour.Black);
        var before = await page.RenderAsync(); await pdf.MovePageAsync(1, 2);
        var after = await page.RenderAsync(); after.PageNumber.Should().Be(2);
        after.PngBytes.ToArray().Should().Equal(before.PngBytes.ToArray());
    }
}
