using FluentAssertions;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements;
using ZingPDF.Graphics;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Text;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Tests.Smoke.TestFiles;
using DrawingCoordinate = ZingPDF.Elements.Drawing.Coordinate;
using DrawingPath = ZingPDF.Elements.Drawing.Path;
using DrawingPathType = ZingPDF.Elements.Drawing.PathType;
using DrawingStrokeOptions = ZingPDF.Elements.Drawing.StrokeOptions;

namespace ZingPDF;

public class PdfTests
{
    [Fact]
    public async Task EncryptedPdf_RequiresAuthentication()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));

        var act = async () => await pdf.GetPageCountAsync();

        var exception = await Assert.ThrowsAnyAsync<Exception>(act);

        exception.GetType().Name.Should().Be("PdfAuthenticationException");
    }

    [Fact]
    public async Task EncryptedPdf_CanBeDecryptedWithPassword()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));

        await pdf.AuthenticateAsync("kanbanery");

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AppendPage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.AppendPageAsync();

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(2);
    }

    [Fact]
    public async Task InsertPage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.InsertPageAsync(1);

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(2);
    }

    [Fact]
    public async Task DeletePage_PageCount()
    {
        using var pdf = Pdf.Create();

        await pdf.DeletePageAsync(1);

        var pageCount = await pdf.GetPageCountAsync();

        pageCount.Should().Be(0);
    }

    [Fact]
    public async Task AppendPage_ManyTimes_StillAllowsRandomPageAccess()
    {
        using var pdf = Pdf.Create();

        for (var i = 0; i < 80; i++)
        {
            await pdf.AppendPageAsync();
        }

        (await pdf.GetPageCountAsync()).Should().Be(81);
        (await pdf.GetPageAsync(1)).Should().NotBeNull();
        (await pdf.GetPageAsync(40)).Should().NotBeNull();
        (await pdf.GetPageAsync(81)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeletePage_AfterManyAppends_PrunesEmptyPageTreeBranches()
    {
        using var pdf = Pdf.Create();

        for (var i = 0; i < 40; i++)
        {
            await pdf.AppendPageAsync();
        }

        for (var i = 0; i < 40; i++)
        {
            await pdf.DeletePageAsync(2);
        }

        (await pdf.GetPageCountAsync()).Should().Be(1);
        (await pdf.GetPageAsync(1)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetPage_PageProperties()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));

        var page = await pdf.GetPageAsync(1);

        page.Dictionary.MediaBox.Should().NotBeNull();
    }

    [Fact]
    public async Task AddWatermarkAsync_SavesModifiedPdf()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();

        await pdf.AddWatermarkAsync("FAST");
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("watermark-minimal.pdf", output);

        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AppendPdfAsync_AppendsPagesFromSecondDocument()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var appendedStream = Files.AsStream(Files.Minimal2);
        using var output = new MemoryStream();

        await pdf.AppendPdfAsync(appendedStream);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("append-minimal1-minimal2.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Page_AddTextAsync_PersistsWrittenContent()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));
        using var output = new MemoryStream();

        var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(200, 200));

        await page.AddTextAsync(new TextObject(
            "test",
            Rectangle.FromDimensions(200, 200),
            new FontOptions
            {
                ResourceName = "Helv",
                Size = 24,
                Colour = RGBColour.PrimaryRed
                    }));

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-text.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);
        var contents = await reloadedPage.Dictionary.Contents.GetAsync();

        (await reloaded.GetPageCountAsync()).Should().BeGreaterThan(1);
        contents.Should().NotBeNull();
        contents!.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page_AddTextAsync_Overload_PersistsWrittenContent()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));
        using var output = new MemoryStream();

        var page = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(200, 200));

        await page.AddTextAsync(
            "test",
            Rectangle.FromDimensions(200, 200),
            new FontOptions
            {
                ResourceName = "Helv",
                Size = 24,
                Colour = RGBColour.PrimaryBlue
            });

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-text-overload.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);
        var contents = await reloadedPage.Dictionary.Contents.GetAsync();

        contents.Should().NotBeNull();
        contents!.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page_AddTextAsync_WithRegisteredStandardFont_WritesFontResource()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "hello",
            Rectangle.FromDimensions(200, 200),
            font,
            18,
            RGBColour.Black);

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-text-registered-standard-font.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/BaseFont /Helvetica");
        writtenPdf.Should().Contain("/Font <<");
        writtenPdf.Should().Contain(" Tf");
    }

    [Fact]
    public async Task Page_AddTextAsync_WithRegisteredTrueTypeFont_WritesEmbeddedFontResource()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterTrueTypeFontAsync(Files.NotoSansRegular);

        await page.AddTextAsync(
            "hello",
            Rectangle.FromDimensions(200, 200),
            font,
            18,
            RGBColour.Black);

        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Subtype /TrueType");
        writtenPdf.Should().Contain("/FontFile2 ");
        writtenPdf.Should().Contain("/BaseFont /NotoSans-Regular");
        writtenPdf.Should().Contain("/FontBBox [-621.000 -389.000 2800.000 1067.000]");
        writtenPdf.Should().NotContain("2,800.000");
    }

    [Fact]
    public async Task Page_AddTextAsync_DefaultLayout_DoesNotClipByDefault()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "hello",
            Rectangle.FromCoordinates(new DrawingCoordinate(40, 120), new DrawingCoordinate(320, 180)),
            font,
            18,
            RGBColour.Black);

        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().NotContain(" re W n");
        writtenPdf.Should().Contain("BT 42");
    }

    [Fact]
    public async Task Page_AddTextAsync_WithClipOverflow_ClipsUsingThePaddedRectangle()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "hello",
            Rectangle.FromCoordinates(new DrawingCoordinate(40, 120), new DrawingCoordinate(320, 180)),
            font,
            18,
            RGBColour.Black,
            new TextLayoutOptions
            {
                Overflow = TextOverflowMode.Clip
            });

        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("42 122 276 56 re W n");
        writtenPdf.Should().Contain("BT 42");
    }

    [Fact]
    public async Task Page_AddTextAsync_WithShrinkToFit_ReducesFontSizeWhenNeeded()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        var font = await pdf.RegisterStandardFontAsync(StandardPdfFonts.Helvetica);

        await page.AddTextAsync(
            "This sentence is intentionally too long for the width at the requested size.",
            Rectangle.FromCoordinates(new DrawingCoordinate(40, 120), new DrawingCoordinate(220, 170)),
            font,
            24,
            RGBColour.Black,
            new TextLayoutOptions
            {
                Overflow = TextOverflowMode.ShrinkToFit
            });

        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        var match = Regex.Match(writtenPdf, @"/[A-Za-z0-9]+\s+([0-9.]+)\s+Tf");
        match.Success.Should().BeTrue();
        double.Parse(match.Groups[1].Value).Should().BeLessThan(24);
    }

    [Fact]
    public async Task Page_AddImageAsync_WritesValidImageXObject()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();
        using var image = Image.FromFile(Files.CatImage, Rectangle.FromDimensions(200, 200));

        var page = await pdf.GetPageAsync(1);

        await page.AddImageAsync(image);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-image.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Subtype /Image");
        writtenPdf.Should().Contain("/Type /XObject");
        writtenPdf.Should().Contain("/Length ");
        writtenPdf.Should().Contain("/Resources <</XObject <<");
        writtenPdf.Should().Contain(" Do");
    }

    [Fact]
    public async Task Page_AddImageAsync_WithPng_WritesFlateImageAndCorrectTransform()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();
        using var pngImage = new SixLabors.ImageSharp.Image<Rgba32>(1, 1, new Rgba32(255, 0, 0, 128));
        using var pngStream = new MemoryStream();
        await pngImage.SaveAsync(pngStream, new PngEncoder());
        pngStream.Position = 0;
        using var image = new Image(
            pngStream,
            Rectangle.FromCoordinates(new DrawingCoordinate(10, 20), new DrawingCoordinate(110, 70)),
            preserveAspectRatio: false);

        var page = await pdf.GetPageAsync(1);

        await page.AddImageAsync(image);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-image-png.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Subtype /Image");
        writtenPdf.Should().Contain("/Filter /FlateDecode");
        writtenPdf.Should().Contain("/ColorSpace /DeviceRGB");
        writtenPdf.Should().Contain("/SMask ");
        writtenPdf.Should().Contain("1 0 0 1 10 20 cm 100 0 0 50 0 0 cm");
    }

    [Fact]
    public async Task Page_AddPathAsync_WritesPathOperations()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);

        await page.AddPathAsync(new DrawingPath(
            new DrawingStrokeOptions(RGBColour.PrimaryRed, 2),
            null,
            DrawingPathType.Linear,
            [
                new DrawingCoordinate(10, 10),
                new DrawingCoordinate(50, 60),
                new DrawingCoordinate(80, 20)
            ]));

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-add-path.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("10 10 m");
        writtenPdf.Should().Contain("50 60 l");
        writtenPdf.Should().Contain("80 20 l");
        writtenPdf.Should().Contain("2 w");
        writtenPdf.Should().Contain("S");
    }

    [Fact]
    public async Task DecompressAsync_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Form));

        await pdf.DecompressAsync();
    }

    [Fact]
    public async Task RemoveHistoryAsync_RewritesPdfWithoutPrevTrailerChain()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedIncrementalHistory));
        using var output = new MemoryStream();

        await pdf.RemoveHistoryAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("remove-history.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().NotContain("/Prev ");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        (await reloaded.GetPageCountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Compress_ImageHeavyFixture_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedImageHeavy));
        using var output = new MemoryStream();

        pdf.Compress(144, 75);
        await pdf.SaveAsync(output);

        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Page_RotateAsync_PersistsPageRotation()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));
        using var output = new MemoryStream();

        var page = await pdf.GetPageAsync(1);
        await page.RotateAsync(Rotation.Degrees90);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("page-rotate.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedPage = await reloaded.GetPageAsync(1);

        ((int)(await reloadedPage.Dictionary.Rotate.GetAsync())!).Should().Be(90);
    }

    [Fact]
    public async Task SetRotationAsync_SavesModifiedPdf()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var output = new MemoryStream();

        await pdf.SetRotationAsync(Rotation.Degrees90);
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("document-rotate.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StreamObject_GetDecompressedDataAsync_CanReadUnfilteredStreamMultipleTimes()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Test));

        var firstStream = await GetFirstStreamObjectAsync(pdf);

        using var firstRead = await firstStream.GetDecompressedDataAsync();
        var firstBytes = await ReadAllBytesAsync(firstRead);

        using var secondRead = await firstStream.GetDecompressedDataAsync();
        var secondBytes = await ReadAllBytesAsync(secondRead);

        secondBytes.Should().Equal(firstBytes);
    }

    [Fact]
    public async Task ExtractTextAsync_TextHeavyFixture_ReturnsNonEmptySegments()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var extracted = (await pdf.ExtractTextAsync()).ToList();

        extracted.Should().NotBeEmpty();
        extracted.Any(x => !string.IsNullOrWhiteSpace(x.Text)).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractTextAsync_PageNumber_ReturnsOnlyRequestedPageSegments()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var firstPageOnly = (await pdf.ExtractTextAsync(1)).ToList();
        var fullDocumentFirstPage = (await pdf.ExtractTextAsync())
            .Where(x => x.PageNumber == 1)
            .ToList();

        firstPageOnly.Should().NotBeEmpty();
        firstPageOnly.Should().OnlyContain(x => x.PageNumber == 1);
        firstPageOnly.Select(x => x.Text).Should().Equal(fullDocumentFirstPage.Select(x => x.Text));
    }

    [Fact]
    public async Task ExtractTextAsync_PlainTextOptions_ReturnExpectedTextForRequestedPage()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var plainText = (await pdf.ExtractTextAsync(1, new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.PlainText
        })).PlainText;

        plainText.Should().NotBeNullOrWhiteSpace();
        plainText.Should().Contain("Tax Invoice");
        plainText.Should().Contain("Thomas Bowers");
        plainText.Should().NotContain("Your Service Summary");
    }

    [Fact]
    public async Task ExtractTextAsync_SegmentOptions_MatchLegacySegmentApi()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var legacy = (await pdf.ExtractTextAsync(1)).ToList();
        var result = await pdf.ExtractTextAsync(1, new TextExtractionOptions
        {
            OutputKind = TextExtractionOutputKind.Segments
        });

        result.Segments.Should().NotBeNull();
        result.Segments!.Select(x => x.Text).Should().Equal(legacy.Select(x => x.Text));
        result.Segments!.Select(x => x.PageNumber).Should().Equal(legacy.Select(x => x.PageNumber));
    }

    [Fact]
    public async Task ExtractTextAsync_PageNumber_RepeatedCallsOnSamePdf_ReturnSameSegments()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var firstRead = (await pdf.ExtractTextAsync(1)).ToList();
        var secondRead = (await pdf.ExtractTextAsync(1)).ToList();

        firstRead.Select(x => x.Text).Should().Equal(secondRead.Select(x => x.Text));
        firstRead.Select(x => x.PageNumber).Should().Equal(secondRead.Select(x => x.PageNumber));
    }

    [Fact]
    public async Task ExtractTextAsync_PageNumber_ThrowsWhenOutOfRange()
    {
        using var pdf = Pdf.Create();

        var act = async () => await pdf.ExtractTextAsync(2);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ExtractTextAsync_GeneratedTextHeavy_FirstPage_ContainsExpectedInvoiceDetailsInOrder()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var firstPageText = string.Join("\n", (await pdf.ExtractTextAsync(1)).Select(x => x.Text));

        firstPageText.Should().Contain("Tax Invoice");
        firstPageText.Should().Contain("Thomas Bowers");
        firstPageText.Should().Contain("Invoice Number:");
        firstPageText.Should().Contain("E68854390");
        AssertContainsInOrder(
            firstPageText,
            "1/545 Queen Street,",
            "Brisbane, QLD 4000",
            "ABN 96 169 263 094",
            "Tax Invoice",
            "Thomas Bowers",
            "Invoice Number:",
            "E68854390");
    }

    [Fact]
    public async Task ExtractTextAsync_GeneratedTextHeavy_PageSpecificExtraction_IsolatesPageContent()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        var firstPageText = string.Join("\n", (await pdf.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await pdf.ExtractTextAsync(2)).Select(x => x.Text));
        var wholeDocumentText = string.Join("\n", (await pdf.ExtractTextAsync()).Select(x => x.Text));

        firstPageText.Should().Contain("Tax Invoice");
        firstPageText.Should().NotContain("Your Service Summary");
        secondPageText.Should().Contain("Your Service Summary");
        secondPageText.Should().Contain("Powered by TCPDF (www.tcpdf.org)");
        secondPageText.Should().NotContain("Tax Invoice");
        wholeDocumentText.Should().Contain("Tax Invoice");
        wholeDocumentText.Should().Contain("Your Service Summary");
    }

    [Fact]
    public async Task ExtractTextAsync_AfterWatermark_InvalidatesCachedPageText()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));

        _ = await pdf.ExtractTextAsync(1);
        await pdf.AddWatermarkAsync("CACHE WATERMARK");

        var firstPageTextAfterMutation = string.Join("\n", (await pdf.ExtractTextAsync(1)).Select(x => x.Text));

        firstPageTextAfterMutation.Should().Contain("CACHE WATERMARK");
    }

    [Fact]
    public async Task ExtractTextAsync_TestPdf_Type0ToUnicodeFixture_DecodesExpectedCompositeFontText()
    {
        var rawPdf = Encoding.ASCII.GetString(Files.ConcurrentRead(Files.Test));
        rawPdf.Should().Contain("/Subtype /Type0");
        rawPdf.Should().Contain("/ToUnicode ");

        using var pdf = Pdf.Load(Files.AsStream(Files.Test));

        var firstPageText = string.Join("\n", (await pdf.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await pdf.ExtractTextAsync(2)).Select(x => x.Text));

        firstPageText.Should().NotContain("\uFFFD");
        secondPageText.Should().NotContain("\uFFFD");

        AssertContainsInOrder(
            firstPageText,
            "1/545 Queen Street,",
            "Brisbane, QLD 4000",
            "Tax Invoice",
            "Thomas Bowers",
            "Invoice Number:",
            "E68854390",
            "Total Owing:",
            "-$62.30");

        AssertContainsInOrder(
            secondPageText,
            "Your Service Summary",
            "Broadband - 0201818095",
            "15 Sep 2023 - 5 Oct 2023",
            "-$62.30",
            "Powered by TCPDF (www.tcpdf.org)");
    }

    [Fact]
    public async Task EncryptAsync_SavesEncryptedPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("secret-password");
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/Encrypt");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("secret-password");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task EncryptAsync_Aes128_SavesEncryptedPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("secret-password", algorithm: PdfEncryptionAlgorithm.Aes128);
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/V 4");
        writtenPdf.Should().Contain("/R 4");
        writtenPdf.Should().Contain("/CFM /AESV2");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("secret-password");
        (await reloaded.GetPageCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EncryptAsync_Aes256_SavesEncryptedPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("secret-password", algorithm: PdfEncryptionAlgorithm.Aes256);
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/V 5");
        writtenPdf.Should().Contain("/R 6");
        writtenPdf.Should().Contain("/CFM /AESV3");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("secret-password");
        (await reloaded.GetPageCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetFormAsync_ExposesPublicButtonFieldTypes()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var fields = await form!.GetFieldsAsync();

        fields.OfType<CheckboxFormField>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFormAsync_FieldBoundsInspection_IsAvailable()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var field = (await form!.GetFieldsAsync()).First();

        var bounds = await field.GetFieldBoundsAsync();
        var dimensions = await field.GetFieldDimensionsAsync();

        bounds.LowerLeft.X.Should().BeGreaterThanOrEqualTo(0);
        bounds.LowerLeft.Y.Should().BeGreaterThanOrEqualTo(0);
        bounds.UpperRight.X.Should().BeGreaterThan(bounds.LowerLeft.X);
        bounds.UpperRight.Y.Should().BeGreaterThan(bounds.LowerLeft.Y);
        bounds.Size.Width.Should().Be(dimensions.Width);
        bounds.Size.Height.Should().Be(dimensions.Height);
    }

    [Fact]
    public async Task CheckboxFormField_SelectOption_PersistsAfterSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var checkbox = (await form!.GetFieldsAsync()).OfType<CheckboxFormField>().First();
        var option = (await checkbox.GetOptionsAsync()).Single();

        option.Selected.Should().BeFalse();

        await option.SelectAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-checkbox-select.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedCheckbox = (await reloadedForm!.GetFieldsAsync()).OfType<CheckboxFormField>().First();
        var reloadedOption = (await reloadedCheckbox.GetOptionsAsync()).Single();

        reloadedOption.Selected.Should().BeTrue();
        reloadedOption.Value.Should().Be(option.Value);
    }

    [Fact]
    public async Task ChoiceFormField_SelectOption_ThenSave_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComboboxForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var choiceField = (await form!.GetFieldsAsync()).OfType<ChoiceFormField>().First();
        var option = (await choiceField.GetOptionsAsync()).First();

        await option.SelectAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-choice-select.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TextFormField_SetValue_PersistsAfterSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        await textField.SetValueAsync("test");
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-text-set-value.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedTextField = (await reloadedForm!.GetFieldsAsync()).OfType<TextFormField>().First(x => x.Name == textField.Name);

        (await reloadedTextField.GetValueAsync()).Should().Be("test");
    }

    [Fact]
    public async Task TextFormField_SetValue_PreservesFixedFontSizeAndWritesClippedAppearance()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var textFields = (await form!.GetFieldsAsync()).OfType<TextFormField>().ToList();

        TextFormField? fixedSizeField = null;
        double originalFontSize = 0;

        foreach (var candidate in textFields)
        {
            var ap = await candidate.GetAPAsync();
            var tf = ap?.Operations.LastOrDefault(x => x.Operator == "Tf");
            if (tf is null)
            {
                continue;
            }

            var size = (double)tf.GetOperand<Number>(1);
            if (size > 0)
            {
                fixedSizeField = candidate;
                originalFontSize = size;
                break;
            }
        }

        fixedSizeField.Should().NotBeNull("the Acrobat-authored fixture should contain at least one fixed-size text field");

        await fixedSizeField!.SetValueAsync("test");

        var updatedAp = await fixedSizeField.GetAPAsync();
        updatedAp.Should().NotBeNull();
        updatedAp!.Operations.Should().Contain(x => x.Operator == "q");
        updatedAp.Operations.Should().Contain(x => x.Operator == "re");
        updatedAp.Operations.Should().Contain(x => x.Operator == "W");
        updatedAp.Operations.Should().Contain(x =>
            x.Operator == "Tf"
            && Math.Abs((double)x.GetOperand<Number>(1) - originalFontSize) < 0.01d);
        updatedAp.Operations.Should().Contain(x =>
            x.Operator == "Td"
            && Math.Abs((double)x.GetOperand<Number>(0) - 2d) < 0.01d);
    }

    [Fact]
    public async Task TextFormField_ClearAsync_ThenSave_DoesNotThrow()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        await textField.SetValueAsync("test");
        await textField.ClearAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-text-clear.pdf", output);
        output.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TextFormField_SetValue_OnCombField_PersistsAndWritesSegmentedAppearance()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Form));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var combField = (await form!.GetFieldsAsync())
            .OfType<TextFormField>()
            .FirstOrDefault(x => x.Properties.IsComb);

        combField.Should().NotBeNull("the Adobe-authored form fixture should contain at least one comb field");

        await combField!.SetValueAsync("1234");

        var updatedAp = await combField.GetAPAsync();
        updatedAp.Should().NotBeNull();
        updatedAp!.Operations.Count(x => x.Operator == "Tj").Should().BeGreaterThan(1);

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-comb-text-set-value.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedField = (await reloadedForm!.GetFieldsAsync())
            .OfType<TextFormField>()
            .First(x => x.Name == combField.Name);

        reloadedField.Properties.IsComb.Should().BeTrue();
        (await reloadedField.GetValueAsync()).Should().Be("1234");
    }

    [Fact]
    public async Task Metadata_CanBeEdited_AndRoundTrips()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var metadata = await pdf.GetMetadataAsync();
        metadata.Title = "Quarterly Report";
        metadata.Author = "Taylor Smith";
        metadata.Subject = "Financial summary";
        metadata.Keywords = "finance,quarterly";
        metadata.Creator = "Integration Test";
        metadata.CreationDate = new DateTimeOffset(2025, 04, 01, 9, 30, 0, TimeSpan.FromHours(10));

        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedMetadata = await reloaded.GetMetadataAsync();

        reloadedMetadata.Title.Should().Be("Quarterly Report");
        reloadedMetadata.Author.Should().Be("Taylor Smith");
        reloadedMetadata.Subject.Should().Be("Financial summary");
        reloadedMetadata.Keywords.Should().Be("finance,quarterly");
        reloadedMetadata.Creator.Should().Be("Integration Test");
        reloadedMetadata.CreationDate.Should().Be(new DateTimeOffset(2025, 04, 01, 9, 30, 0, TimeSpan.FromHours(10)));
    }

    [Fact]
    public async Task SaveAsync_StampsProducerWithZingPdf()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var metadata = await reloaded.GetMetadataAsync();

        metadata.Producer.Should().Be(PdfMetadata.ProducerName);
        metadata.ModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_AllowsOwnerPasswordAuthentication()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync("user-secret", "owner-secret");
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("owner-secret");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task EncryptAsync_ThenDecryptAsync_RoundTripsToPlainPdf()
    {
        using var pdf = Pdf.Create();
        using var encryptedOutput = new MemoryStream();

        await pdf.EncryptAsync("secret-password");
        await pdf.SaveAsync(encryptedOutput);

        encryptedOutput.Position = 0;
        using var encryptedPdf = Pdf.Load(encryptedOutput);

        await encryptedPdf.DecryptAsync("secret-password");

        using var decryptedOutput = new MemoryStream();
        await encryptedPdf.SaveAsync(decryptedOutput);

        decryptedOutput.Position = 0;
        using var reloaded = Pdf.Load(decryptedOutput);
        var trailer = await reloaded.Objects.GetLatestTrailerDictionaryAsync();

        (await trailer.Encrypt.GetAsync()).Should().BeNull();

        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    private static async Task<IStreamObject> GetFirstStreamObjectAsync(Pdf pdf)
    {
        await foreach (var obj in pdf.Objects)
        {
            if (obj.Object is IStreamObject streamObject)
            {
                return streamObject;
            }
        }

        throw new InvalidOperationException("Expected a stream object in the PDF.");
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }

    private static void AssertContainsInOrder(string value, params string[] fragments)
    {
        var currentIndex = 0;

        foreach (var fragment in fragments)
        {
            var foundIndex = value.IndexOf(fragment, currentIndex, StringComparison.Ordinal);
            foundIndex.Should().BeGreaterThanOrEqualTo(0, $"expected to find '{fragment}' after index {currentIndex}");
            currentIndex = foundIndex + fragment.Length;
        }
    }

    private static async Task WriteArtifactAsync(string fileName, MemoryStream output)
    {
        var artifactDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "manual-verification");
        Directory.CreateDirectory(artifactDirectory);

        var artifactPath = Path.Combine(artifactDirectory, fileName);

        output.Position = 0;
        await File.WriteAllBytesAsync(artifactPath, output.ToArray());
    }
}
