using System.Text;
using FluentAssertions;
using ZingPDF.Elements;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Graphics;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;
using ZingPDF.Text.Editing;
using Xunit;
using PdfRectangle = ZingPDF.Syntax.CommonDataStructures.Rectangle;

namespace ZingPDF.Tests.Unit;

[System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
[System.Runtime.Versioning.SupportedOSPlatform("ios13.6")]
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("maccatalyst13.5")]
[System.Runtime.Versioning.SupportedOSPlatform("macos")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class PdfTextEditorTests
{
    [Theory]
    [InlineData("Tj")]
    [InlineData("TJ")]
    public async Task ReplaceLine_PreservesFollowingLineAndPageColor_AfterSave(string showing)
    {
        using var pdf = Pdf.Create(o => o.MediaBox = PdfRectangle.FromDimensions(300, 200));
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 14, RGBColour.Black));
        var text = showing == "TJ" ? "[(Original) -100 ( line)] TJ" : "(Original line) Tj";
        await SetContentAsync(pdf, page, $"0.8 0.9 0.95 rg 0 0 300 200 re f q 0.7 0.1 0.2 rg BT /F1 14 Tf 1.2 Tc 2 Tw 80 Tz 18 TL 1 0 0 1 30 120 Tm {text} T* (Following) Tj ET Q");
        var editor = pdf.CreateTextEditor();
        var lines = await editor.GetLinesAsync(1);
        lines.Should().HaveCount(2);
        var following = lines.Single(l => l.Text == "Following").Bounds;
        var original = lines.Single(l => l.Text == "Original line");
        (await editor.CheckReplacementAsync(original, "Changed")).CanApply.Should().BeTrue();
        await editor.ReplaceLineAsync(original, "Changed");
        using var output = new MemoryStream(); await pdf.SaveAsync(output);
        output.Position = 0; using var saved = Pdf.Load(output);
        var after = await saved.CreateTextEditor().GetLinesAsync(1);
        after.Select(l => l.Text).Should().Contain("Changed").And.Contain("Following").And.NotContain("Original line");
        var next = after.Single(l => l.Text == "Following").Bounds;
        next.LowerLeft.X.Should().BeApproximately(following.LowerLeft.X, .001);
        next.LowerLeft.Y.Should().BeApproximately(following.LowerLeft.Y, .001);
        var render = await saved.RenderPageAsync(1);
        using var pixels = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(render.PngBytes.Span);
        pixels[5, 5].R.Should().BeInRange(195, 210);
    }

    [Fact]
    public async Task ReplacingWithGoogleCompatibleTrueType_EmbedsProgramAndUnicode_AndChecksOverflow()
    {
        using var pdf = Pdf.Create(o => o.MediaBox = PdfRectangle.FromDimensions(300, 200));
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        var page = await pdf.GetPageAsync(1);
        await page.AddTextAsync(new TextObject("Name", new Coordinate(25, 100), font, 14, RGBColour.PrimaryBlue));
        var editor = pdf.CreateTextEditor();
        var line = (await editor.GetLinesAsync(1)).Single();
        (await editor.CheckReplacementAsync(line, "Name \u03A9")).RequiresFontChange.Should().BeTrue();
        var data = await File.ReadAllBytesAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "TestFonts", "NotoSans-Regular.ttf"));
        var options = new PdfTextReplacementOptions { TrueTypeFontData = data };
        var check = await editor.CheckReplacementAsync(line, "Name \u03A9", options);
        check.CanApply.Should().BeTrue(check.Message);
        (await editor.CheckReplacementAsync(line, new string('W', 100), options)).WouldOverflow.Should().BeTrue();
        await editor.ReplaceLineAsync(line, "Name \u03A9", options);
        using var output = new MemoryStream(); await pdf.SaveAsync(output);
        output.Position = 0; using var saved = Pdf.Load(output);
        var lines = await saved.CreateTextEditor().GetLinesAsync(1);
        lines.Single().Text.Should().Be("Name \u03A9"); lines.Single().IsFontEmbedded.Should().BeTrue();
        (await saved.ExtractTextAsync(1)).Select(t => t.Text).Should().Contain(t => t.Contains("Name \u03A9"));
        var rendered = await saved.RenderPageAsync(1);
        rendered.PngBytes.Length.Should().BeGreaterThan(100);
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.ReplaceLineAsync(line, "Stale"));
    }

    [Fact]
    public async Task SharedFormXObject_OnlyEditedOccurrenceChanges()
    {
        using var pdf = Pdf.Create(o => o.MediaBox = PdfRectangle.FromDimensions(300, 200));
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 12, RGBColour.Black));
        var resources = await page.Dictionary.Resources.GetAsync();
        var dictionary = new StreamDictionary(pdf, ObjectContext.UserCreated);
        dictionary.Set("Type", (Name)"XObject"); dictionary.Set("Subtype", (Name)"Form");
        dictionary.Set("BBox", PdfRectangle.FromDimensions(100, 40)); dictionary.Set("Resources", resources);
        var form = await AddStreamAsync(pdf, "BT /F1 12 Tf 1 0 0 1 5 15 Tm (Repeated) Tj ET", dictionary);
        await page.Dictionary.AddXObjectResourceAsync("Fx", form, pdf);
        await SetContentAsync(pdf, page, "q 1 0 0 1 10 30 cm /Fx Do Q q 1 0 0 1 10 100 cm /Fx Do Q");
        var editor = pdf.CreateTextEditor();
        var lines = await editor.GetLinesAsync(1); lines.Should().HaveCount(2);
        await editor.ReplaceLineAsync(lines[0], "Changed");
        using var output = new MemoryStream(); await pdf.SaveAsync(output);
        output.Position = 0; using var saved = Pdf.Load(output);
        var result = await saved.CreateTextEditor().GetLinesAsync(1);
        result.Select(l => l.Text).Should().BeEquivalentTo("Changed", "Repeated");
    }

    [Fact]
    public async Task FullEmbeddedFont_ExtendsMappingAutomatically_ButSubsetRequiresChoice()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);
        await page.AddTextAsync(new TextObject("Name", new Coordinate(25, 100), font, 14, RGBColour.Black));
        var bytes = await File.ReadAllBytesAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "TestFonts", "NotoSans-Regular.ttf"));
        var editor = pdf.CreateTextEditor();
        await editor.ReplaceLineAsync((await editor.GetLinesAsync(1)).Single(), "Name", new() { TrueTypeFontData = bytes });
        using var firstSave = new MemoryStream(); await pdf.SaveAsync(firstSave); firstSave.Position = 0;
        using var saved = Pdf.Load(firstSave);
        editor = saved.CreateTextEditor();
        var line = (await editor.GetLinesAsync(1)).Single();
        var check = await editor.CheckReplacementAsync(line, "Name \u03A9");
        check.CanApply.Should().BeTrue(check.Message); check.WillEmbedFont.Should().BeTrue();
        await editor.ReplaceLineAsync(line, "Name \u03A9");
        (await editor.GetLinesAsync(1)).Single().Text.Should().Be("Name \u03A9");

        var resources = await (await saved.GetPageAsync(1)).Dictionary.Resources.GetAsync();
        var fonts = await new ResourceDictionary(resources!).Font.GetAsync();
        foreach (var resource in fonts!.InnerDictionary.Keys.ToArray())
        {
            var dictionary = await fonts.GetRequiredProperty<FontDictionary>(resource).GetAsync();
            if ((await dictionary.BaseFont.GetAsync())?.Value.Contains("Noto") == true)
            {
                dictionary.Set("BaseFont", (Name)"ABCDEF+NotoSans");
                if (fonts.InnerDictionary[resource] is ZingPDF.Syntax.Objects.IndirectObjects.IndirectObjectReference reference)
                    saved.Objects.Update(await saved.Objects.GetAsync(reference));
            }
        }
        line = (await editor.GetLinesAsync(1)).Single();
        line.IsFontSubset.Should().BeTrue();
        check = await editor.CheckReplacementAsync(line, "Name \u0416");
        check.RequiresFontChange.Should().BeTrue(); check.CanApply.Should().BeFalse();
        // A subset name must never be treated as evidence that its original family is available.
        (await editor.CheckReplacementAsync(line, "Name \u0416", new() { TrueTypeFontData = bytes })).CanApply.Should().BeTrue();
    }

    [Theory]
    [InlineData("(Edited) '")]
    [InlineData("3 1 (Edited) \"")]
    public async Task QuoteOperators_PreserveLaterRelativeLinePosition(string source)
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 12, RGBColour.Black));
        await SetContentAsync(pdf, page, $"BT /F1 12 Tf 18 TL 1 0 0 1 30 180 Tm (Before) Tj {source} T* (After) Tj ET");
        var editor = pdf.CreateTextEditor();
        var before = await editor.GetLinesAsync(1);
        var following = before.Single(l => l.Text == "After").Bounds;
        await editor.ReplaceLineAsync(before.Single(l => l.Text == "Edited"), "A much longer line");
        var after = await editor.GetLinesAsync(1);
        AssertSameBounds(after.Single(l => l.Text == "After").Bounds, following);
    }

    [Theory]
    [InlineData(0)] [InlineData(90)] [InlineData(180)] [InlineData(270)]
    public async Task SplitRuns_CropRotationAndTransform_PreserveSurroundingPixels(int rotation)
    {
        using var pdf = Pdf.Create(o => o.MediaBox = PdfRectangle.FromDimensions(300, 300));
        var page = await pdf.GetPageAsync(1);
        page.Dictionary.Set("CropBox", PdfRectangle.FromCoordinates(new(10, 20), new(280, 290)));
        page.Dictionary.Set("Rotate", (Number)rotation);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 12, RGBColour.Black));
        await SetContentAsync(pdf, page, "q .9 .8 .7 rg 10 20 270 270 re f Q q .8 .1 -.1 .8 20 20 cm BT /F1 12 Tf 1 0 0 1 30 160 Tm (Split) Tj ( line) Tj 0 -24 Td (Keep me) Tj ET Q");
        var editor = pdf.CreateTextEditor();
        var lines = await editor.GetLinesAsync(1); lines.Should().HaveCount(2);
        var selected = lines.Single(l => l.Text == "Split line");
        var keep = lines.Single(l => l.Text == "Keep me");
        var initial = await pdf.RenderPageAsync(1);
        var reject = await editor.CheckReplacementAsync(selected, new string('W', 200)); reject.CanApply.Should().BeFalse();
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.ReplaceLineAsync(selected, new string('W', 200)));
        (await pdf.RenderPageAsync(1)).PngBytes.ToArray().Should().Equal(initial.PngBytes.ToArray());
        await editor.ReplaceLineAsync(selected, "New");
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var reopened = Pdf.Load(output);
        var result = await reopened.CreateTextEditor().GetLinesAsync(1);
        result.Select(l => l.Text).Should().BeEquivalentTo("New", "Keep me");
        AssertSameBounds(result.Single(l => l.Text == "Keep me").Bounds, keep.Bounds);
        var rendered = await reopened.RenderPageAsync(1);
        using var oldPixels = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(initial.PngBytes.Span);
        using var newPixels = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(rendered.PngBytes.Span);
        var geometry = await page.GetGeometryAsync();
        var display = new[] { selected.Bounds.LowerLeft, selected.Bounds.UpperRight,
            new Coordinate(selected.Bounds.LowerLeft.X, selected.Bounds.UpperRight.Y), new Coordinate(selected.Bounds.UpperRight.X, selected.Bounds.LowerLeft.Y) }.Select(geometry.PageToDisplay).ToArray();
        var scale = (double)oldPixels.Width / geometry.DisplayWidth;
        var minX = display.Min(p => p.X) * scale - 4; var maxX = display.Max(p => p.X) * scale + 4;
        var minY = display.Min(p => p.Y) * scale - 4; var maxY = display.Max(p => p.Y) * scale + 4;
        for (var y = 0; y < oldPixels.Height; y++)
            for (var x = 0; x < oldPixels.Width; x++)
                if (x < minX || x > maxX || y < minY || y > maxY)
                    newPixels[x, y].Should().Be(oldPixels[x, y], $"unaffected pixel {x},{y} at rotation {rotation}");
    }

    [Theory]
    [InlineData("27", "\u2019")]
    [InlineData("60", "\u2018")]
    [InlineData("AE", "\uFB01")]
    [InlineData("A4", "\u2044")]
    [InlineData("F9", "\u00F8")]
    public async Task StandardType1WithoutEncoding_UsesStandardEncoding_NotPdfDoc(string code, string expected)
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 12, RGBColour.Black));
        var dictionary = await pdf.Objects.GetAsync<FontDictionary>(font.FontReference);
        dictionary.InnerDictionary.Remove("Encoding");
        await SetContentAsync(pdf, page, $"BT /F1 12 Tf 1 0 0 1 30 100 Tm <{code}> Tj ET");
        var line = (await pdf.CreateTextEditor().GetLinesAsync(1)).Single();
        line.Text.Should().Be(expected);
        (await pdf.ExtractTextAsync(1)).Select(t => t.Text).Should().Contain(expected);
        (await pdf.CreateTextEditor().CheckReplacementAsync(line, expected)).CanApply.Should().BeTrue();
    }

    [Fact]
    public async Task ActualTextMarkedContent_IsUneditable_WithoutDisablingOtherLines()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync(new TextObject("", Coordinate.Zero, font, 12, RGBColour.Black));
        await SetContentAsync(pdf, page, "/Span << /ActualText (Logical) >> BDC BT /F1 12 Tf 1 0 0 1 30 100 Tm (Painted) Tj ET EMC BT /F1 12 Tf 1 0 0 1 30 160 Tm (Normal) Tj ET");
        var reference = await page.Dictionary.Contents.GetRawValueAsync();
        var editor = pdf.CreateTextEditor();
        var lines = await editor.GetLinesAsync(1);
        var marked = lines.Single(l => l.Text == "Painted");
        marked.CanEdit.Should().BeFalse(); marked.UnsupportedReason.Should().Contain("ActualText");
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.ReplaceLineAsync(marked, "Blocked"));
        lines.Single(l => l.Text == "Normal").CanEdit.Should().BeTrue();
        (await page.Dictionary.Contents.GetRawValueAsync()).Should().Be(reference);
    }

    [Fact]
    public async Task AdjacentStreams_FontAliasesAndRepeatedColor_GroupAndReplaceAsOneLine()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync("", Coordinate.Zero, font, 12, RGBColour.Black);
        var resources = new ResourceDictionary((await page.Dictionary.Resources.GetAsync())!);
        await resources.AddFontAsync("F2", font.FontReference, pdf); page.Dictionary.Set("Resources", resources);
        var first = await AddStreamAsync(pdf, "BT /F1 12 Tf 0 0 0 rg 1 0 0 1 30 180 Tm (Hello) Tj", new StreamDictionary(pdf, ObjectContext.UserCreated));
        var second = await AddStreamAsync(pdf, "0 0 0 rg /F2 12 Tf ( world) Tj 0 -24 Td (Following) Tj ET", new StreamDictionary(pdf, ObjectContext.UserCreated));
        page.Dictionary.Set("Contents", new ArrayObject([first, second], ObjectContext.UserCreated));
        var editor = pdf.CreateTextEditor();
        var before = await editor.GetLinesAsync(1);
        before.Select(l => l.Text).Should().Equal("Hello world", "Following");
        var bytes = await File.ReadAllBytesAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "TestFonts", "NotoSans-Regular.ttf"));
        await editor.ReplaceLineAsync(before[0], "Replaced \u03A9", new() { TrueTypeFontData = bytes });
        using var output = new MemoryStream(); await pdf.SaveAsync(output); output.Position = 0;
        using var saved = Pdf.Load(output);
        var after = await saved.CreateTextEditor().GetLinesAsync(1);
        after.Select(l => l.Text).Should().Equal("Replaced \u03A9", "Following");
        AssertSameBounds(after[1].Bounds, before[1].Bounds);
    }

    [Fact]
    public async Task GraphicsStateFont_IsReportedLocally_AndLaterExplicitFontRemainsEditable()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica, "F1");
        await page.AddTextAsync("", Coordinate.Zero, font, 12, RGBColour.Black);
        var resources = new ResourceDictionary((await page.Dictionary.Resources.GetAsync())!);
        var graphics = new ZingPDF.Syntax.Objects.Dictionaries.Dictionary(pdf, ObjectContext.UserCreated);
        graphics.Set("Font", new ArrayObject([font.FontReference, (Number)12], ObjectContext.UserCreated));
        var states = new ZingPDF.Syntax.Objects.Dictionaries.Dictionary(pdf, ObjectContext.UserCreated);
        states.Set("GS", graphics); resources.Set("ExtGState", states); page.Dictionary.Set("Resources", resources);
        await SetContentAsync(pdf, page, "/GS gs BT 1 0 0 1 30 180 Tm (Blocked) Tj ET BT /F1 12 Tf 1 0 0 1 30 140 Tm (Editable) Tj ET");
        var editor = pdf.CreateTextEditor();
        var lines = await editor.GetLinesAsync(1);
        lines.Single(l => l.Text == "Blocked").CanEdit.Should().BeFalse();
        await editor.ReplaceLineAsync(lines.Single(l => l.Text == "Editable"), "Changed");
        (await editor.GetLinesAsync(1)).Select(l => l.Text).Should().Equal("Blocked", "Changed");
    }

    private static void AssertSameBounds(PdfRectangle actual, PdfRectangle expected)
    {
        actual.LowerLeft.X.Should().BeApproximately(expected.LowerLeft.X, .001);
        actual.LowerLeft.Y.Should().BeApproximately(expected.LowerLeft.Y, .001);
        actual.UpperRight.X.Should().BeApproximately(expected.UpperRight.X, .001);
        actual.UpperRight.Y.Should().BeApproximately(expected.UpperRight.Y, .001);
    }

    private static async Task SetContentAsync(Pdf pdf, Page page, string content)
    {
        page.Dictionary.Set("Contents", await AddStreamAsync(pdf, content, new StreamDictionary(pdf, ObjectContext.UserCreated)));
        pdf.Objects.Update(page.IndirectObject);
    }
    private static async Task<ZingPDF.Syntax.Objects.IndirectObjects.IndirectObjectReference> AddStreamAsync(Pdf pdf, string content, StreamDictionary dictionary)
    {
        var bytes = Encoding.ASCII.GetBytes(content); dictionary.Set("Length", (Number)bytes.Length);
        return (await pdf.Objects.AddAsync(new StreamObject<StreamDictionary>(new MemoryStream(bytes), dictionary))).Reference;
    }
}
