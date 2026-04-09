using FluentAssertions;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements;
using ZingPDF.Graphics;
using ZingPDF.Graphics.Images;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Text;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Tests.Smoke.TestFiles;
using ZingPDF.OCR;
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
    public async Task InsertDeleteAndAppendPdfAsync_PreservesExpectedPageSequence()
    {
        using var source = new MemoryStream();
        using var mergeSource = new MemoryStream();
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("alpha").HelveticaBold().FontSize(18).At(20, 140)))
            .Page(page => page.Size(240, 180).Text(text => text.Value("bravo").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(source);

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("merged page").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(mergeSource);

        source.Position = 0;
        mergeSource.Position = 0;

        using (var pdf = Pdf.Load(source))
        {
            var insertedPage = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(240, 180));
            await insertedPage.AddTextAsync(
                "inserted page",
                Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(20, 120), new ZingPDF.Elements.Drawing.Coordinate(200, 150)),
                await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
                18,
                RGBColour.Black);

            await pdf.DeletePageAsync(3);
            await pdf.AppendPdfAsync(mergeSource);
            await pdf.SaveAsync(output);
        }

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(3);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await reloaded.ExtractTextAsync(2)).Select(x => x.Text));
        var thirdPageText = string.Join("\n", (await reloaded.ExtractTextAsync(3)).Select(x => x.Text));
        var wholeDocumentText = string.Join("\n", (await reloaded.ExtractTextAsync()).Select(x => x.Text));

        firstPageText.Should().Contain("inserted page");
        secondPageText.Should().Contain("alpha");
        thirdPageText.Should().Contain("merged page");
        wholeDocumentText.Should().NotContain("bravo");
    }

    [Fact]
    public async Task WrappedInsertDeleteAndAppendPdfAsync_PreservesExpectedPageSequence()
    {
        using var source = new MemoryStream();
        using var mergeSource = new MemoryStream();
        using var result = new MemoryStream();
        using var wrapped = new MemoryStream();

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("alpha").HelveticaBold().FontSize(18).At(20, 140)))
            .Page(page => page.Size(240, 180).Text(text => text.Value("bravo").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(source);

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("merged page").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(mergeSource);

        source.Position = 0;
        mergeSource.Position = 0;

        using (var pdf = Pdf.Load(source))
        {
            var insertedPage = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(240, 180));
            await insertedPage.AddTextAsync(
                "inserted page",
                Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(20, 120), new ZingPDF.Elements.Drawing.Coordinate(200, 150)),
                await pdf.RegisterStandardFontAsync(StandardPdfFonts.HelveticaBold),
                18,
                RGBColour.Black);

            await pdf.DeletePageAsync(3);
            await pdf.AppendPdfAsync(mergeSource);
            await pdf.SaveAsync(result);
        }

        using (var wrapperSource = new MemoryStream())
        {
            await Pdf.New()
                .Page(page => page.Size(240, 180).Text(text => text.Value("instructions").HelveticaBold().FontSize(18).At(20, 140)))
                .SaveAsync(wrapperSource);

            wrapperSource.Position = 0;
            result.Position = 0;

            using var wrapperPdf = Pdf.Load(wrapperSource);
            await wrapperPdf.AppendPdfAsync(result);
            await wrapperPdf.SaveAsync(wrapped);
        }

        wrapped.Position = 0;
        using var reloaded = Pdf.Load(wrapped);

        (await reloaded.GetPageCountAsync()).Should().Be(4);

        var wholeDocumentText = string.Join("\n", (await reloaded.ExtractTextAsync()).Select(x => x.Text));

        wholeDocumentText.Should().Contain("instructions");
        wholeDocumentText.Should().Contain("inserted page");
        wholeDocumentText.Should().Contain("alpha");
        wholeDocumentText.Should().Contain("merged page");
        wholeDocumentText.Should().NotContain("bravo");
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
    public async Task AppendPdfAsync_AppendedDocumentText_CanBeReadAfterSave()
    {
        using var basePdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        using var sourcePdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        using var appendedStream = Files.AsStream(Files.GeneratedTextHeavy);
        using var output = new MemoryStream();

        var basePageCount = await basePdf.GetPageCountAsync();
        var appendedPageCount = await sourcePdf.GetPageCountAsync();

        await basePdf.AppendPdfAsync(appendedStream);
        await basePdf.SaveAsync(output);
        await WriteArtifactAsync("append-minimal-text-heavy.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(basePageCount + appendedPageCount);

        var firstAppendedPageText = (await reloaded.ExtractTextAsync(
            basePageCount + 1,
            new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText }))
            .PlainText;

        firstAppendedPageText.Should().Contain("Tax Invoice");
        firstAppendedPageText.Should().Contain("Thomas Bowers");
    }

    [Fact]
    public async Task AppendPdfAsync_MergedDocument_CanBeSavedAgainWithoutLosingAppendedContent()
    {
        using var basePdf = Pdf.Load(Files.AsStream(Files.GeneratedMixedWorkload));
        using var sourcePdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        using var appendedStream = Files.AsStream(Files.GeneratedTextHeavy);
        using var firstOutput = new MemoryStream();

        var basePageCount = await basePdf.GetPageCountAsync();
        var appendedPageCount = await sourcePdf.GetPageCountAsync();

        await basePdf.AppendPdfAsync(appendedStream);
        await basePdf.SaveAsync(firstOutput);

        firstOutput.Position = 0;
        using var reloaded = Pdf.Load(firstOutput);
        using var secondOutput = new MemoryStream();

        await reloaded.SaveAsync(secondOutput);
        await WriteArtifactAsync("append-mixed-text-heavy-resave.pdf", secondOutput);

        secondOutput.Position = 0;
        using var savedAgain = Pdf.Load(secondOutput);

        (await savedAgain.GetPageCountAsync()).Should().Be(basePageCount + appendedPageCount);

        var firstAppendedPageText = (await savedAgain.ExtractTextAsync(
            basePageCount + 1,
            new TextExtractionOptions { OutputKind = TextExtractionOutputKind.PlainText }))
            .PlainText;

        firstAppendedPageText.Should().Contain("Tax Invoice");
        firstAppendedPageText.Should().Contain("Thomas Bowers");
    }

    [Fact]
    public async Task ExportPagesAsync_SelectedPages_PreserveRequestedOrderAfterSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        using var exported = await pdf.ExportPagesAsync([2, 1]);
        using var output = new MemoryStream();

        await exported.SaveAsync(output);
        await WriteArtifactAsync("export-pages-generated-text-heavy.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(2);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await reloaded.ExtractTextAsync(2)).Select(x => x.Text));

        firstPageText.Should().Contain("Your Service Summary");
        firstPageText.Should().NotContain("Tax Invoice");
        secondPageText.Should().Contain("Tax Invoice");
        secondPageText.Should().Contain("Thomas Bowers");
    }

    [Fact]
    public async Task WrappedExportPagesAsync_SelectedPages_PreserveRequestedOrderAfterSave()
    {
        using var source = new MemoryStream();
        using var wrapperSource = new MemoryStream();
        using var exportedOutput = new MemoryStream();
        using var wrappedOutput = new MemoryStream();

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("page one").HelveticaBold().FontSize(18).At(20, 140)))
            .Page(page => page.Size(240, 180).Text(text => text.Value("page two").HelveticaBold().FontSize(18).At(20, 140)))
            .Page(page => page.Size(240, 180).Text(text => text.Value("page three").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(source);

        source.Position = 0;
        using (var pdf = Pdf.Load(source))
        using (var exported = await pdf.ExportPagesAsync([3, 1]))
        {
            await exported.SaveAsync(exportedOutput);
        }

        await Pdf.New()
            .Page(page => page.Size(240, 180).Text(text => text.Value("instructions").HelveticaBold().FontSize(18).At(20, 140)))
            .SaveAsync(wrapperSource);

        wrapperSource.Position = 0;
        exportedOutput.Position = 0;

        using (var wrapperPdf = Pdf.Load(wrapperSource))
        {
            await wrapperPdf.AppendPdfAsync(exportedOutput);
            await wrapperPdf.SaveAsync(wrappedOutput);
        }

        wrappedOutput.Position = 0;
        using var reloaded = Pdf.Load(wrappedOutput);

        (await reloaded.GetPageCountAsync()).Should().Be(3);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await reloaded.ExtractTextAsync(2)).Select(x => x.Text));
        var thirdPageText = string.Join("\n", (await reloaded.ExtractTextAsync(3)).Select(x => x.Text));
        var wholeDocumentText = string.Join("\n", (await reloaded.ExtractTextAsync()).Select(x => x.Text));

        firstPageText.Should().Contain("instructions");
        secondPageText.Should().Contain("page three");
        thirdPageText.Should().Contain("page one");
        wholeDocumentText.Should().NotContain("page two");
    }

    [Fact]
    public async Task SplitAsync_ReturnsDocumentsWithRequestedPageCount()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        var parts = await pdf.SplitAsync(10);

        try
        {
            parts.Should().HaveCount(2);
            (await parts[0].GetPageCountAsync()).Should().Be(10);
            (await parts[1].GetPageCountAsync()).Should().Be(10);

            using var firstOutput = new MemoryStream();
            using var output = new MemoryStream();
            await parts[0].SaveAsync(firstOutput);
            await parts[1].SaveAsync(output);

            firstOutput.Length.Should().BeGreaterThan(0);
            output.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            foreach (var part in parts)
            {
                part.Dispose();
            }
        }
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
    public async Task PdfNew_FluentBuilder_CreatesPdfWithTextAndRectangle()
    {
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Text(text => text
                    .Value("hello from fluent")
                    .Font(StandardPdfFonts.Helvetica)
                    .FontSize(18)
                    .At(20, 140))
                .Rectangle(box => box
                    .At(20, 40)
                    .Size(80, 30)
                    .Stroke(RGBColour.PrimaryBlue, 2)
                    .Fill(new RGBColour(0.9, 0.97, 1))))
            .SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(1);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        firstPageText.Should().Contain("hello from fluent");

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain(" 20 40 m ");
        writtenPdf.Should().Contain(" B ");
    }

    [Fact]
    public async Task PdfNew_FluentBuilder_SupportsLinePathImageWatermarkAndTrueTypeFont()
    {
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(240, 240)
                .Text(text => text
                    .Value("fluent coverage")
                    .WithTrueTypeFont(Files.NotoSansRegular, fontName: "NotoSans-Regular")
                    .FontSize(16)
                    .At(20, 200))
                .Line(line => line
                    .From(20, 190)
                    .To(120, 190)
                    .Stroke(RGBColour.PrimaryRed, 2))
                .Path(path => path
                    .Linear()
                    .Point(20, 40)
                    .Point(80, 40)
                    .Point(50, 90)
                    .Point(20, 40)
                    .Stroke(RGBColour.PrimaryBlue, 2)
                    .Fill(new RGBColour(0.85, 0.93, 1)))
                .Image(image => image
                    .FromFile(Files.CatImage)
                    .At(130, 30)
                    .Size(70, 70))
                .Watermark("DRAFT"))
            .SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/FontFile2 ");
        writtenPdf.Should().Contain("/Subtype /Image");
        writtenPdf.Should().Contain("(DRAFT)");
        writtenPdf.Should().Contain(" 20 190 m ");
        writtenPdf.Should().Contain(" 120 190 l ");
        writtenPdf.Should().Contain(" 50 90 l ");
    }

    [Fact]
    public async Task PdfNew_FluentBuilder_SupportsBoundedTextLayout()
    {
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(240, 180)
                .Rectangle(box => box
                    .At(20, 40)
                    .Size(140, 48)
                    .Stroke(RGBColour.Black, 1))
                .Text(text => text
                    .Value("bounded text")
                    .HelveticaBold()
                    .FontSize(18)
                    .InBox(20, 40, 140, 48)
                    .AlignCenter()
                    .AlignMiddle()
                    .Padding(0)
                    .ClipOverflow()))
            .SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        var pageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        pageText.Should().Contain("bounded text");

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain(" re W n");
    }

    [Fact]
    public async Task PdfPages_FluentEditingBuilder_CanModifyExistingPagesAndAppendPages()
    {
        using var source = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(240, 180)
                .Text(text => text
                    .Value("first page")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 140)))
            .Page(page => page
                .Size(240, 180)
                .Text(text => text
                    .Value("second page")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 140)))
            .SaveAsync(source);

        source.Position = 0;
        using var loaded = Pdf.Load(source);
        using var output = new MemoryStream();

        await loaded.Pages(pages => pages
                .Page(1, page => page
                    .Text(text => text
                        .Value("edited")
                        .Helvetica()
                        .FontSize(12)
                        .At(20, 100)))
                .Append(page => page
                    .Size(240, 180)
                    .Text(text => text
                        .Value("appended page")
                        .HelveticaBold()
                        .FontSize(18)
                        .At(20, 140))))
            .SaveAsync(output);

        await WriteArtifactAsync("pages-fluent-editing.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(3);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await reloaded.ExtractTextAsync(2)).Select(x => x.Text));
        var thirdPageText = string.Join("\n", (await reloaded.ExtractTextAsync(3)).Select(x => x.Text));

        firstPageText.Should().Contain("first page");
        firstPageText.Should().Contain("edited");
        secondPageText.Should().Contain("second page");
        thirdPageText.Should().Contain("appended page");
    }

    [Fact]
    public async Task PdfPages_FluentEditingBuilder_CanRemovePages()
    {
        using var source = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(240, 180)
                .Text(text => text
                    .Value("first page")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 140)))
            .Page(page => page
                .Size(240, 180)
                .Text(text => text
                    .Value("second page")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 140)))
            .SaveAsync(source);

        source.Position = 0;
        using var loaded = Pdf.Load(source);
        using var output = new MemoryStream();

        await loaded.Pages(pages => pages
                .Remove(2))
            .SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetPageCountAsync()).Should().Be(1);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        firstPageText.Should().Contain("first page");
    }

    [Fact]
    public async Task PdfRedactionPlan_CanMarkExactTextAndApplyOverlay()
    {
        using var source = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(240, 180)
                .Text(text => text
                    .Value("Account number: 12345")
                    .HelveticaBold()
                    .FontSize(16)
                    .At(20, 120)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);
        using var output = new MemoryStream();

        var plan = await pdf.RedactionAsync();
        var markCount = await plan.MarkTextAsync("12345");
        var report = await plan.ApplyAsync(new PdfRedactionOptions
        {
            OverlayText = "REDACTED"
        });

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("redaction-text-overlay.pdf", output);

        markCount.Should().Be(1);
        report.AppliedMarkCount.Should().Be(1);
        report.PagesTouched.Should().ContainSingle().Which.Should().Be(1);
        report.Warnings.Should().NotBeEmpty();

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("REDACTED");
        writtenPdf.Should().NotContain("12345");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var extractedText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        extractedText.Should().NotContain("12345");
    }

    [Fact]
    public async Task PdfRedactionPlan_CanMarkRegionAndRewriteOutput()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Text(text => text
                    .Value("Top Secret")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 120)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);

        var plan = await pdf.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(18, 112),
                new DrawingCoordinate(120, 138)));

        var report = await plan.ApplyAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("redaction-region-overlay.pdf", output);

        report.AppliedMarkCount.Should().Be(1);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain(" f ");
        writtenPdf.Should().NotContain("Top Secret");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var extractedText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        extractedText.Should().NotContain("Top Secret");
    }

    [Fact]
    public async Task PdfRedactionPlan_RegionMark_CanRedactPagesWithVectorPainting()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Rectangle(box => box
                    .At(20, 20)
                    .Size(60, 40)
                    .Fill(RGBColour.PrimaryBlue))
                .Text(text => text
                    .Value("Secret")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 120)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);

        var plan = await pdf.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(18, 18),
                new DrawingCoordinate(90, 70)));

        await plan.ApplyAsync();
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("20 20 m 80 20 l 80 60 l 20 60 l 20 20 l n");
        writtenPdf.Should().NotContain(" R G ");
    }

    [Fact]
    public async Task PdfRedactionPlan_RegionMark_PreservesVisibleTextOutsideTheMarkedRegion()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Rectangle(box => box
                    .At(20, 20)
                    .Size(60, 40)
                    .Fill(RGBColour.PrimaryBlue))
                .Text(text => text
                    .Value("Secret")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 120)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);

        var plan = await pdf.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(18, 18),
                new DrawingCoordinate(90, 70)));

        await plan.ApplyAsync(new PdfRedactionOptions
        {
            OverlayText = "REDACTED"
        });
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("(Secret)");
        writtenPdf.Should().Contain("(REDACTED)");
        writtenPdf.Should().NotContain(" R G ");

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var extractedText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        extractedText.Should().Contain("Secret");
    }

    [Fact]
    public async Task PdfRedactionPlan_TextAndRegionMarks_DoNotLeaveOriginalContentInRewrittenOutput()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Rectangle(box => box
                    .At(20, 20)
                    .Size(60, 40)
                    .Fill(RGBColour.PrimaryBlue))
                .Text(text => text
                    .Value("Secret")
                    .HelveticaBold()
                    .FontSize(18)
                    .At(20, 120)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);

        var plan = await pdf.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(18, 18),
                new DrawingCoordinate(90, 70)));

        await plan.MarkTextAsync("Secret");
        await plan.ApplyAsync(new PdfRedactionOptions
        {
            OverlayText = "REDACTED"
        });

        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().NotContain("(Secret)");
        writtenPdf.Should().NotContain("20 20 m 80 20 l 80 60 l 20 60 l 20 20 l B");
        writtenPdf.Should().Contain("(REDACTED)");
        writtenPdf.Should().Contain("20 20 m 80 20 l 80 60 l 20 60 l 20 20 l n");
        writtenPdf.Should().Contain("20 120 Td (      ) Tj");
    }

    [Fact]
    public async Task PdfRedactionPlan_RegionMark_CanRedactImageXObjects()
    {
        using var source = new MemoryStream();
        using var output = new MemoryStream();
        using var pngImage = new SixLabors.ImageSharp.Image<Rgba32>(1, 1, new Rgba32(255, 0, 0, 255));
        using var pngStream = new MemoryStream();

        await pngImage.SaveAsync(pngStream, new PngEncoder());
        pngStream.Position = 0;

        await Pdf.New()
            .Page(page => page
                .Size(200, 200)
                .Image(image => image
                    .FromStream(() => new MemoryStream(pngStream.ToArray(), writable: false))
                    .At(20, 20)
                    .Size(100, 100)))
            .SaveAsync(source);

        source.Position = 0;
        using var pdf = Pdf.Load(source);

        var plan = await pdf.RedactionAsync();
        plan.MarkRegion(
            1,
            Rectangle.FromCoordinates(
                new DrawingCoordinate(18, 18),
                new DrawingCoordinate(122, 122)));

        await plan.ApplyAsync();
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var page = await reloaded.GetPageAsync(1);
        var rawResources = await page.Dictionary.Resources.GetAsync();
        var resources = ResourceDictionary.FromDictionary(rawResources!);
        var xObjects = await resources.XObject.GetAsync();
        var imageReference = (IndirectObjectReference)xObjects!.First().Value;
        var imageStream = await reloaded.Objects.GetAsync<StreamObject<ImageDictionary>>(imageReference);
        await using var decoded = await imageStream.GetDecompressedDataAsync();
        using var decodedCopy = new MemoryStream();
        await decoded.CopyToAsync(decodedCopy);

        decodedCopy.ToArray().Take(3).Should().Equal((byte)0, (byte)0, (byte)0);
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
    public async Task ExtractTextWithOcrAsync_PrefersEmbeddedText_WhenAvailable()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        var engine = new DelegateOcrEngine(_ => throw new InvalidOperationException("OCR should not be called when embedded text exists."));

        var result = await pdf.ExtractTextWithOcrAsync(1, engine);

        result.UsedEmbeddedText.Should().BeTrue();
        result.UsedOcr.Should().BeFalse();
        result.Text.Should().Contain("Tax Invoice");
    }

    [Fact]
    public async Task ExtractTextWithOcrAsync_UsesLargestPageImage_WhenNoEmbeddedTextExists()
    {
        using var pdf = Pdf.Create();
        var page = await pdf.GetPageAsync(1);
        using var output = new MemoryStream();

        await page.AddImageAsync(Files.CatImage, Rectangle.FromDimensions(200, 200));
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        OcrInputImage? seenImage = null;
        var engine = new DelegateOcrEngine(image =>
        {
            seenImage = image;
            return $"ocr:{image.Width}x{image.Height}:{image.MimeType}";
        });

        var result = await reloaded.ExtractTextWithOcrAsync(1, engine, new PdfOcrOptions
        {
            PreferEmbeddedText = false,
            ThrowWhenNoOcrCandidate = true
        });

        result.UsedEmbeddedText.Should().BeFalse();
        result.UsedOcr.Should().BeTrue();
        result.Text.Should().StartWith("ocr:");
        seenImage.Should().NotBeNull();
        seenImage!.PageNumber.Should().Be(1);
        seenImage.Data.Should().NotBeEmpty();
        result.SourceImageWidth.Should().Be(seenImage.Width);
        result.SourceImageHeight.Should().Be(seenImage.Height);
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

    private sealed class DelegateOcrEngine(Func<OcrInputImage, string> handler) : IOcrEngine
    {
        public Task<string> RecognizeAsync(OcrInputImage image, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(handler(image));
        }
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
    public async Task EncryptAsync_WritesRequestedPermissionsToEncryptionDictionary()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var permissions = PdfEncryptionPermissions.Print | PdfEncryptionPermissions.Copy;

        await pdf.EncryptAsync(
            "secret-password",
            algorithm: PdfEncryptionAlgorithm.Aes256,
            permissions: permissions);
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain($"/P {ToStandardPermissionValue(permissions)}");
    }

    [Fact]
    public async Task EncryptAsync_PrintHighQuality_AlsoSetsPrintPermission()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        await pdf.EncryptAsync(
            "secret-password",
            algorithm: PdfEncryptionAlgorithm.Aes256,
            permissions: PdfEncryptionPermissions.PrintHighQuality);
        await pdf.SaveAsync(output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain($"/P {ToStandardPermissionValue(PdfEncryptionPermissions.PrintHighQuality)}");
    }

    [Fact]
    public async Task SignatureFormField_SignAsync_WritesDetachedSignatureDictionary()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();
        using var certificate = CreateSigningCertificate();

        await AddSignatureFieldAsync(pdf, "ApprovalSignature");

        var form = await pdf.GetFormAsync();
        var signatureField = (await form!.GetFieldsAsync()).OfType<SignatureFormField>().First();

        await signatureField.SignAsync(certificate, new PdfSignatureOptions
        {
            SignerName = "Taylor Smith",
            Reason = "Approval",
            Location = "Sydney"
        });

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-signed.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/ByteRange [");
        writtenPdf.Should().Contain("/SubFilter /adbe.pkcs7.detached");
        writtenPdf.Should().Contain("/AP <<");
        AssertDetachedSignatureVerifies(output.ToArray());

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedSignatureField = (await reloadedForm!.GetFieldsAsync()).OfType<SignatureFormField>().First();

        (await reloadedSignatureField.HasSignatureValueAsync()).Should().BeTrue();
        (await reloadedSignatureField.GetFilterAsync()).Should().Be("Adobe.PPKLite");
        (await reloadedSignatureField.GetSubFilterAsync()).Should().Be("adbe.pkcs7.detached");
        (await reloadedSignatureField.GetSignerNameAsync()).Should().Be("Taylor Smith");
        (await reloadedSignatureField.GetReasonAsync()).Should().Be("Approval");
    }

    [Fact]
    public async Task SignatureFormField_SignAsync_WithSignatureImage_WritesImageIntoVisibleAppearance()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();
        using var certificate = CreateSigningCertificate();

        await AddSignatureFieldAsync(pdf, "ApprovalSignature");

        var form = await pdf.GetFormAsync();
        var signatureField = (await form!.GetFieldsAsync()).OfType<SignatureFormField>().First();

        await signatureField.SignAsync(certificate, new PdfSignatureOptions
        {
            SignerName = "Taylor Smith",
            Reason = "Approval",
            SignatureImageBytes = Files.ConcurrentRead(Files.CatImage)
        });

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-signed-with-image.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/AP <<");
        writtenPdf.Should().Contain("/Subtype /Image");
        writtenPdf.Should().Contain("/SigImage");
    }

    [Fact]
    public async Task SignInvisibleAsync_AddsHiddenSignatureField_WhenDocumentHasNoExistingField()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();
        using var certificate = CreateSigningCertificate();

        await pdf.SignInvisibleAsync(certificate, new PdfSignatureOptions
        {
            FieldName = "ServerSignature",
            SignerName = "Taylor Smith",
            Reason = "Integrity check"
        });

        await pdf.SaveAsync(output);
        await WriteArtifactAsync("signature-hidden-field.pdf", output);

        var writtenPdf = Encoding.ASCII.GetString(output.ToArray());
        writtenPdf.Should().Contain("/FT /Sig");
        writtenPdf.Should().Contain("/ByteRange [");
        writtenPdf.Should().Contain("/Rect [0.000 0.000 0.000 0.000]");
        writtenPdf.Should().Contain("/F 34");
        writtenPdf.Should().NotContain("/AP <<");
        AssertDetachedSignatureVerifies(output.ToArray());

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedSignatureField = (await reloadedForm!.GetFieldsAsync()).OfType<SignatureFormField>().Single();

        reloadedSignatureField.Name.Should().Be("ServerSignature");
        (await reloadedSignatureField.HasSignatureValueAsync()).Should().BeTrue();
        (await reloadedSignatureField.GetSignerNameAsync()).Should().Be("Taylor Smith");
        (await reloadedSignatureField.GetReasonAsync()).Should().Be("Integrity check");
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
    public async Task GetFieldAsync_ByName_ReturnsMatchingField()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var expectedField = (await form!.GetFieldsAsync()).First();

        var field = await form.GetFieldAsync(expectedField.Name);

        field.Should().NotBeNull();
        field!.Name.Should().Be(expectedField.Name);
    }

    [Fact]
    public async Task GetFieldAsync_TypedLookup_ReturnsMatchingType()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var expectedField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        var field = await form.GetFieldAsync<TextFormField>(expectedField.Name);

        field.Should().NotBeNull();
        field!.Name.Should().Be(expectedField.Name);
    }

    [Fact]
    public async Task GetFieldAsync_TypedLookup_ReturnsNull_WhenFieldTypeDoesNotMatch()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        var field = await form.GetFieldAsync<ChoiceFormField>(textField.Name);

        field.Should().BeNull();
    }

    [Fact]
    public async Task GetFieldAsync_ReturnsNull_WhenNameDoesNotExist()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));

        var form = await pdf.GetFormAsync();

        var field = await form!.GetFieldAsync("Does.Not.Exist");

        field.Should().BeNull();
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
    public async Task Page_AddWatermarkAsync_AffectsOnlyTheSelectedPage()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.GeneratedTextHeavy));
        using var output = new MemoryStream();

        var firstPage = await pdf.GetPageAsync(1);

        await firstPage.AddWatermarkAsync("PAGE ONE ONLY");
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("watermark-first-page-only.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        var firstPageText = string.Join("\n", (await reloaded.ExtractTextAsync(1)).Select(x => x.Text));
        var secondPageText = string.Join("\n", (await reloaded.ExtractTextAsync(2)).Select(x => x.Text));

        firstPageText.Should().Contain("PAGE ONE ONLY");
        secondPageText.Should().NotContain("PAGE ONE ONLY");
    }

    [Fact]
    public async Task ChoiceFormField_SelectOptionByTextAsync_ReturnsTrue_WhenOptionExists()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComboboxForm));

        var form = await pdf.GetFormAsync();
        var choiceField = (await form!.GetFieldsAsync()).OfType<ChoiceFormField>().First();
        var option = (await choiceField.GetOptionsAsync()).First();
        var optionText = option.Text.Decode();

        var selected = await choiceField.SelectOptionByTextAsync(optionText);

        selected.Should().BeTrue();
    }

    [Fact]
    public async Task ChoiceFormField_SelectOptionByValueAsync_ReturnsFalse_WhenOptionDoesNotExist()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComboboxForm));

        var form = await pdf.GetFormAsync();
        var choiceField = (await form!.GetFieldsAsync()).OfType<ChoiceFormField>().First();

        var selected = await choiceField.SelectOptionByValueAsync("DoesNotExist");

        selected.Should().BeFalse();
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
    public async Task Form_FlattenAsync_RemovesInteractiveFormStructure()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.ComplexForm));
        using var output = new MemoryStream();

        var form = await pdf.GetFormAsync();
        var textField = (await form!.GetFieldsAsync()).OfType<TextFormField>().First();

        await textField.SetValueAsync("flattened");
        await form.FlattenAsync();
        await pdf.SaveAsync(output);
        await WriteArtifactAsync("form-flattened.pdf", output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        (await reloaded.GetFormAsync()).Should().BeNull();

        var pageCount = await reloaded.GetPageCountAsync();
        for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
        {
            var page = await reloaded.GetPageAsync(pageNumber);
            var annotations = await page.Dictionary.Annots.GetAsync();

            foreach (var annotationRef in annotations?.OfType<IndirectObjectReference>() ?? [])
            {
                var annotationObject = await reloaded.Objects.GetAsync(annotationRef);
                annotationObject.Object.Should().NotBeOfType<WidgetAnnotationDictionary>();
            }
        }
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
    public async Task GetOrCreateFormAsync_AddTextFieldAsync_CreatesDiscoverableField()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        await form.AddTextFieldAsync(
            1,
            "CustomerName",
            Rectangle.FromCoordinates(
                new ZingPDF.Elements.Drawing.Coordinate(40, 90),
                new ZingPDF.Elements.Drawing.Coordinate(260, 120)),
            options => options.Description = "Customer name");

        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var textField = await reloadedForm!.GetFieldAsync<TextFormField>("CustomerName");

        textField.Should().NotBeNull();
        textField!.Description.Should().Be("Customer name");
        ((double)(await textField.GetFieldBoundsAsync()).Width).Should().Be(220);
    }

    [Fact]
    public async Task CreatedTextField_SetValueAsync_PersistsAfterSave()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        var createdField = await form.AddTextFieldAsync(
            1,
            "CustomerName",
            Rectangle.FromCoordinates(
                new ZingPDF.Elements.Drawing.Coordinate(40, 90),
                new ZingPDF.Elements.Drawing.Coordinate(260, 120)));

        await createdField.SetValueAsync("validated");
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var textField = await reloadedForm!.GetFieldAsync<TextFormField>("CustomerName");

        textField.Should().NotBeNull();
        (await textField!.GetValueAsync()).Should().Be("validated");
        (await textField.GetAPAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateFormAsync_AddCheckboxRadioChoiceAndSignatureFields_CreatesDiscoverableFields()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        await form.AddCheckboxFieldAsync(1, "AcceptTerms", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 700), new ZingPDF.Elements.Drawing.Coordinate(56, 716)));
        await form.AddRadioButtonFieldAsync(
            1,
            "DeliveryOption",
            [
                new RadioButtonFieldOption("Standard", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 660), new ZingPDF.Elements.Drawing.Coordinate(56, 676))),
                new RadioButtonFieldOption("Express", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 632), new ZingPDF.Elements.Drawing.Coordinate(56, 648)))
            ]);
        await form.AddComboBoxFieldAsync(
            1,
            "Priority",
            Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 590), new ZingPDF.Elements.Drawing.Coordinate(180, 614)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("Medium"), new ChoiceFieldOption("High")]);
        await form.AddListBoxFieldAsync(
            1,
            "Regions",
            Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 520), new ZingPDF.Elements.Drawing.Coordinate(180, 572)),
            [new ChoiceFieldOption("APAC"), new ChoiceFieldOption("EMEA"), new ChoiceFieldOption("NA")]);
        await form.AddSignatureFieldAsync(1, "ApprovalSignature", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 450), new ZingPDF.Elements.Drawing.Coordinate(240, 500)));
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var fields = (await reloadedForm!.GetFieldsAsync()).ToList();

        fields.OfType<CheckboxFormField>().Single().Name.Should().Be("AcceptTerms");
        fields.OfType<RadioButtonFormField>().Single().Name.Should().Be("DeliveryOption");
        fields.OfType<ComboBoxFormField>().Single().Name.Should().Be("Priority");
        fields.OfType<ListBoxFormField>().Single().Name.Should().Be("Regions");
        fields.OfType<SignatureFormField>().Single().Name.Should().Be("ApprovalSignature");
    }

    [Fact]
    public async Task CreatedCheckboxRadioAndChoiceFields_PersistSelectionsAfterSave()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        var checkbox = await form.AddCheckboxFieldAsync(1, "AcceptTerms", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 700), new ZingPDF.Elements.Drawing.Coordinate(56, 716)));
        var radio = await form.AddRadioButtonFieldAsync(
            1,
            "DeliveryOption",
            [
                new RadioButtonFieldOption("Standard", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 660), new ZingPDF.Elements.Drawing.Coordinate(56, 676))),
                new RadioButtonFieldOption("Express", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 632), new ZingPDF.Elements.Drawing.Coordinate(56, 648)))
            ]);
        var combo = await form.AddComboBoxFieldAsync(
            1,
            "Priority",
            Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 590), new ZingPDF.Elements.Drawing.Coordinate(180, 614)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("Medium"), new ChoiceFieldOption("High")]);

        await (await checkbox.GetOptionsAsync()).Single().SelectAsync();
        await (await radio.GetOptionsAsync()).Single(x => x.Value == "Express").SelectAsync();
        (await combo.SelectOptionByValueAsync("High")).Should().BeTrue();
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();

        (await (await reloadedForm!.GetFieldAsync<CheckboxFormField>("AcceptTerms"))!.GetOptionsAsync()).Single().Selected.Should().BeTrue();
        (await (await reloadedForm.GetFieldAsync<RadioButtonFormField>("DeliveryOption"))!.GetOptionsAsync()).Single(x => x.Value == "Express").Selected.Should().BeTrue();
        (await (await reloadedForm.GetFieldAsync<ComboBoxFormField>("Priority"))!.GetOptionByValueAsync("High"))!.Selected.Should().BeTrue();
    }

    [Fact]
    public async Task CreatedCheckboxAndRadioFields_UseCanonicalWidgetAppearanceStructure()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        await form.AddCheckboxFieldAsync(1, "AcceptTerms", Rectangle.FromCoordinates(new DrawingCoordinate(40, 700), new DrawingCoordinate(56, 716)));
        await form.AddRadioButtonFieldAsync(
            1,
            "DeliveryOption",
            [
                new RadioButtonFieldOption("Standard", Rectangle.FromCoordinates(new DrawingCoordinate(40, 660), new DrawingCoordinate(56, 676))),
                new RadioButtonFieldOption("Express", Rectangle.FromCoordinates(new DrawingCoordinate(40, 632), new DrawingCoordinate(56, 648)))
            ]);
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var trailer = await reloaded.Objects.GetLatestTrailerDictionaryAsync();
        var catalogObject = await reloaded.Objects.GetAsync(trailer.Root!);
        var catalog = (DocumentCatalogDictionary)catalogObject.Object;
        var acroFormDictionary = await catalog.AcroForm.GetAsync();
        var rootFieldReferences = await acroFormDictionary!.Fields.GetAsync();
        var rootFields = new List<IndirectObject>();
        foreach (var fieldRef in rootFieldReferences.Cast<IndirectObjectReference>())
        {
            rootFields.Add(await reloaded.Objects.GetAsync(fieldRef));
        }

        FieldDictionary? checkboxDictionary = null;
        FieldDictionary? radioRoot = null;
        foreach (var rootField in rootFields.Select(x => (FieldDictionary)x.Object))
        {
            var fieldName = (await rootField.T.GetAsync())!.Decode();
            if (fieldName == "AcceptTerms")
            {
                checkboxDictionary = rootField;
            }
            else if (fieldName == "DeliveryOption")
            {
                radioRoot = rootField;
            }
        }

        checkboxDictionary.Should().NotBeNull();
        radioRoot.Should().NotBeNull();

        (await checkboxDictionary!.MK.GetAsync()).Should().BeNull();
        (await checkboxDictionary.H.GetAsync())!.Value.Should().Be("N");
        var checkboxAppearance = await checkboxDictionary.AP.GetAsync();
        checkboxAppearance.Should().NotBeNull();
        var checkboxAppearanceNotNull = checkboxAppearance!;
        (await checkboxAppearanceNotNull.R.GetAsync()).Value.Should().BeNull();
        (await checkboxAppearanceNotNull.D.GetAsync()).Value.Should().BeNull();
        var checkboxNormalStates = (await checkboxAppearanceNotNull.N.GetAsync()).Type2!;
        checkboxNormalStates.Keys.Should().BeEquivalentTo(["Off", "Yes"]);

        var radioKids = await radioRoot!.Kids.GetAsync();
        radioKids.Should().NotBeNull();
        foreach (var kidRef in radioKids!.Cast<IndirectObjectReference>())
        {
            var kidObject = await reloaded.Objects.GetAsync(kidRef);
            var widget = (FieldDictionary)kidObject.Object;
            (await widget.MK.GetAsync()).Should().BeNull();
            (await widget.H.GetAsync())!.Value.Should().Be("N");
            var widgetAppearance = await widget.AP.GetAsync();
            widgetAppearance.Should().NotBeNull();
            var widgetAppearanceNotNull = widgetAppearance!;
            (await widgetAppearanceNotNull.R.GetAsync()).Value.Should().BeNull();
            (await widgetAppearanceNotNull.D.GetAsync()).Value.Should().BeNull();
            var widgetStates = (await widgetAppearanceNotNull.N.GetAsync()).Type2!;
            widgetStates.Keys.Should().Contain("Off");
            widgetStates.Keys.Should().ContainSingle(key => key != "Off");
        }
    }

    [Fact]
    public async Task CreatedMixedFields_TextFieldCanStillRenderAppearanceAfterReload()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();

        var form = await pdf.GetOrCreateFormAsync();
        await form.AddTextFieldAsync(1, "CustomerName", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 700), new ZingPDF.Elements.Drawing.Coordinate(200, 724)));
        await form.AddCheckboxFieldAsync(1, "AcceptTerms", Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 660), new ZingPDF.Elements.Drawing.Coordinate(56, 676)));
        await form.AddComboBoxFieldAsync(
            1,
            "Priority",
            Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 620), new ZingPDF.Elements.Drawing.Coordinate(180, 644)),
            [new ChoiceFieldOption("Low"), new ChoiceFieldOption("High")]);
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var textField = await reloadedForm!.GetFieldAsync<TextFormField>("CustomerName");

        await textField!.SetValueAsync("validated");
        await reloaded.SaveAsync(new MemoryStream());

        (await textField.GetAPAsync()).Should().NotBeNull();
    }

    [Fact]
    public async Task CreatedSignatureField_SignsSuccessfully()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream();
        var form = await pdf.GetOrCreateFormAsync();
        var signatureField = await form.AddSignatureFieldAsync(
            1,
            "ApprovalSignature",
            Rectangle.FromCoordinates(new ZingPDF.Elements.Drawing.Coordinate(40, 450), new ZingPDF.Elements.Drawing.Coordinate(240, 500)));
        using var certificate = CreateSigningCertificate();

        await signatureField.SignAsync(certificate, new PdfSignatureOptions
        {
            SignerName = "Smoke Test",
            Reason = "Created field validation"
        });
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);
        var reloadedForm = await reloaded.GetFormAsync();
        var reloadedSignatureField = await reloadedForm!.GetFieldAsync<SignatureFormField>("ApprovalSignature");

        (await reloadedSignatureField!.HasSignatureValueAsync()).Should().BeTrue();
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

    private static X509Certificate2 CreateSigningCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=ZingPDF Smoke Test Signer",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));
    }

    private static async Task AddSignatureFieldAsync(Pdf pdf, string fieldName)
    {
        var trailer = await pdf.Objects.GetLatestTrailerDictionaryAsync();
        var catalogObject = await pdf.Objects.GetAsync(trailer.Root!);
        var catalog = (DocumentCatalogDictionary)catalogObject.Object;

        var formDictionary = InteractiveFormDictionary.FromDictionary(new Dictionary<string, IPdfObject>
        {
            ["Fields"] = new ArrayObject([], ObjectContext.UserCreated)
        }, pdf, ObjectContext.UserCreated);
        var formObject = await pdf.Objects.AddAsync(formDictionary);
        catalog.Set("AcroForm", formObject.Reference);
        pdf.Objects.Update(catalogObject);

        var page = await pdf.GetPageAsync(1);
        var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
        {
            ["Type"] = (Name)"Annot",
            ["Subtype"] = (Name)AnnotationDictionary.Subtypes.Widget,
            ["FT"] = (Name)"Sig",
            ["T"] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
            ["Rect"] = Rectangle.FromDimensions(200, 60),
            ["P"] = page.Dictionary,
        }, pdf, ObjectContext.UserCreated);

        var fieldObject = await pdf.Objects.AddAsync(fieldDictionary);
        (await formDictionary.Fields.GetAsync()).Add(fieldObject.Reference);

        var annotations = await page.Dictionary.Annots.GetAsync() ?? new ArrayObject([], ObjectContext.UserCreated);
        annotations.Add(fieldObject.Reference);
        page.Dictionary.Set("Annots", annotations);
        pdf.Objects.Update(page.IndirectObject);
        pdf.Objects.Update(formObject);
    }

    private static int ToStandardPermissionValue(PdfEncryptionPermissions permissions)
    {
        if ((permissions & PdfEncryptionPermissions.PrintHighQuality) != 0)
        {
            permissions |= PdfEncryptionPermissions.Print;
        }

        return unchecked((int)0xFFFFF0C0) | (int)permissions;
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

    private static void AssertDetachedSignatureVerifies(byte[] pdfBytes)
    {
        var ascii = Encoding.ASCII.GetString(pdfBytes);
        var byteRangeMatch = Regex.Match(ascii, @"/ByteRange \[(\d+) (\d+) (\d+) (\d+)\]");
        byteRangeMatch.Success.Should().BeTrue("expected the signed PDF to contain a ByteRange entry");

        var range0 = int.Parse(byteRangeMatch.Groups[1].Value);
        var range1 = int.Parse(byteRangeMatch.Groups[2].Value);
        var range2 = int.Parse(byteRangeMatch.Groups[3].Value);
        var range3 = int.Parse(byteRangeMatch.Groups[4].Value);

        var signedContent = new byte[range1 + range3];
        Buffer.BlockCopy(pdfBytes, range0, signedContent, 0, range1);
        Buffer.BlockCopy(pdfBytes, range2, signedContent, range1, range3);

        var contentsStartMarker = "/Contents <";
        var contentsStart = ascii.IndexOf(contentsStartMarker, StringComparison.Ordinal);
        contentsStart.Should().BeGreaterThanOrEqualTo(0, "expected the signed PDF to contain signature contents");

        var hexStart = contentsStart + contentsStartMarker.Length;
        var hexEnd = ascii.IndexOf('>', hexStart);
        hexEnd.Should().BeGreaterThan(hexStart, "expected the signature contents hex string to terminate");

        var allSignatureBytes = new byte[(hexEnd - hexStart) / 2];
        for (var i = 0; i < allSignatureBytes.Length; i++)
        {
            allSignatureBytes[i] = Convert.ToByte(ascii.Substring(hexStart + (i * 2), 2), 16);
        }

        AsnDecoder.ReadEncodedValue(
            allSignatureBytes,
            AsnEncodingRules.BER,
            out _,
            out _,
            out var consumed);

        var cmsBytes = allSignatureBytes.AsSpan(0, consumed).ToArray();
        var cms = new SignedCms(new ContentInfo(signedContent), detached: true);
        cms.Decode(cmsBytes);
        cms.CheckHash();
        cms.CheckSignature(verifySignatureOnly: true);
    }
}
