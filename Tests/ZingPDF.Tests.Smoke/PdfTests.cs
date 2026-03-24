using FluentAssertions;
using Xunit;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Parsing;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Tests.Smoke.TestFiles;

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
        var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));

        var pageCount1 = await pdf.GetPageCountAsync();

        _ = await pdf.AppendPageAsync();

        var pageCount2 = await pdf.GetPageCountAsync();

        pageCount2.Should().Be(pageCount1 + 1);
    }

    [Fact]
    public async Task AppendPage_CanRetrieveAfterAdding()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));

        _ = await pdf.AppendPageAsync();

        var addedPage = await pdf.GetPageAsync(2);

        addedPage.Should().NotBeNull();
    }

    [Fact]
    public async Task InsertPage_PageCount()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));

        var pageCount1 = await pdf.GetPageCountAsync();

        _ = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(100, 100));

        var pageCount2 = await pdf.GetPageCountAsync();

        pageCount2.Should().Be(pageCount1 + 1);
    }

    [Fact]
    public async Task InsertPage_InsertsAtRequestedIndex()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        var originalFirstPage = await pdf.GetPageAsync(1);

        _ = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromDimensions(100, 100));

        var currentFirstPage = await pdf.GetPageAsync(1);
        var currentSecondPage = await pdf.GetPageAsync(2);

        currentFirstPage.IndirectObject.Id.Should().NotBe(originalFirstPage.IndirectObject.Id);
        currentSecondPage.IndirectObject.Id.Should().Be(originalFirstPage.IndirectObject.Id);
    }

    [Fact]
    public async Task AppendPdf_AddsAllPages()
    {
        var pdf = Pdf.Load(Files.AsStream(Files.Minimal1));
        var originalPageCount = await pdf.GetPageCountAsync();

        using var appendStream = Files.AsStream(Files.Minimal2);
        using var appendedPdf = Pdf.Load(Files.AsStream(Files.Minimal2));
        var appendedPageCount = await appendedPdf.GetPageCountAsync();

        await pdf.AppendPdfAsync(appendStream);

        var mergedPageCount = await pdf.GetPageCountAsync();

        mergedPageCount.Should().Be(originalPageCount + appendedPageCount);
    }

    [Fact]
    public async Task Create_CreatesSingleBlankPage()
    {
        using var pdf = Pdf.Create();

        var pageCount = await pdf.GetPageCountAsync();
        var page = await pdf.GetPageAsync(1);

        pageCount.Should().Be(1);
        page.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveEncryptedPdf_PreservesEncryptionForNewObjects()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));
        using var output = new MemoryStream();

        await pdf.AuthenticateAsync("kanbanery");
        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(100, 100));
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("kanbanery");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DecryptAsync_RemovesEncryptionOnSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));
        using var output = new MemoryStream();

        await pdf.DecryptAsync("kanbanery");
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        var pageCount = await reloaded.GetPageCountAsync();

        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EncryptAsync_PreservesEncryptionOnSave()
    {
        using var pdf = Pdf.Load(Files.AsStream(Files.Encrypted));
        using var output = new MemoryStream();

        await pdf.AuthenticateAsync("kanbanery");
        await pdf.EncryptAsync("kanbanery");
        _ = await pdf.AppendPageAsync(options => options.MediaBox = Rectangle.FromDimensions(100, 100));
        await pdf.SaveAsync(output);

        output.Position = 0;
        using var reloaded = Pdf.Load(output);

        await reloaded.AuthenticateAsync("kanbanery");
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EncryptAsync_EncryptsPreviouslyUnencryptedPdf()
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
        using var decryptedOutput = new MemoryStream();

        await encryptedPdf.DecryptAsync("secret-password");
        await encryptedPdf.SaveAsync(decryptedOutput);

        decryptedOutput.Position = 0;
        using var reloaded = Pdf.Load(decryptedOutput);
        var pageCount = await reloaded.GetPageCountAsync();
        pageCount.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_RejectsNonEmptyForeignOutputStream()
    {
        using var pdf = Pdf.Create();
        using var output = new MemoryStream(Encoding.ASCII.GetBytes("not a pdf"));

        var act = async () => await pdf.SaveAsync(output);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AddWatermark_WritesWatermarkContent()
    {
        using var pdf = Pdf.Create();

        await pdf.AddWatermarkAsync("ABCDEF");
        var page = await pdf.GetPageAsync(1);
        var contents = await page.Dictionary.Contents.GetRawValueAsync();
        var contentReference = contents switch
        {
            IndirectObjectReference singleRef => singleRef,
            ArrayObject ary => ary.Cast<IndirectObjectReference>().Last(),
            _ => throw new InvalidOperationException("Unexpected page contents structure.")
        };

        var contentStream = await pdf.Objects.GetAsync(contentReference);
        var streamObject = (IStreamObject)contentStream.Object;
        using var data = await streamObject.GetDecompressedDataAsync();
        using var reader = new StreamReader(data);
        var contentText = await reader.ReadToEndAsync();

        contentText.Should().Contain("ABCDEF");
    }

    [Fact]
    public async Task Compress_AddsFlateFilterToUncompressedContentStream()
    {
        using var pdf = Pdf.Create();

        await pdf.AddWatermarkAsync("TEST");
        pdf.Compress(72, 70);

        var page = await pdf.GetPageAsync(1);
        var contents = await page.Dictionary.Contents.GetRawValueAsync();
        var contentReference = contents switch
        {
            IndirectObjectReference singleRef => singleRef,
            ArrayObject ary => ary.Cast<IndirectObjectReference>().Last(),
            _ => throw new InvalidOperationException("Unexpected page contents structure.")
        };

        var contentStream = await pdf.Objects.GetAsync(contentReference);
        var filters = await ((IStreamObject)contentStream.Object).Dictionary.Filter.GetAsync();

        filters.Should().NotBeNull();
        filters!.Cast<ZingPDF.Syntax.Objects.Name>().Select(x => x.Value).Should().Contain("FlateDecode");
    }


    //    [Fact]
    //    public async Task SimpleIncrementalUpdate()
    //    {
    //        var pdf = Pdf.Load(File.Open("TestFiles/minimal.pdf", FileMode.Open));

    //        var outputStream = File.Open("output.pdf", FileMode.Create);

    //        await pdf.AppendPageAsync();

    //        await pdf.SaveAsync(outputStream);

    //        outputStream.Position = 0;
    //        var output = await outputStream.GetAsync();

    //        var expectedOutput = "%PDF-2.0\r\n" +
    //            "%����\r\n" +
    //            "1 0 obj\r\n" +
    //            "<</Type /Catalog/Pages 2 0 R>>\r\n" +
    //            "endobj\r\n" +
    //            "2 0 obj\r\n" +
    //            "<</Type /Pages/Kids [3 0 R]/Count 1>>\r\n" +
    //            "endobj\r\n" +
    //            "3 0 obj\r\n" +
    //            "<</Type /Page/Parent 2 0 R/Resources <<>>>>\r\n" +
    //            "endobj\r\n" +
    //            "xref\r\n" +
    //            "0 4\r\n" +
    //            "0000000000 65535 f\r\n" +
    //            "0000000017 00000 n\r\n" +
    //            "0000000066 00000 n\r\n" +
    //            "0000000122 00000 n\r\n" +
    //            "trailer\r\n" +
    //            "<</Size 4/Root 1 0 R/ID [<2045e2246d17437290c929c74954eb23> <2045e2246d17437290c929c74954eb23>]>>\r\n" +
    //            "startxref\r\n" +
    //            "184\r\n" +
    //            "%%EOF\r\n" +
    //            "2 0 obj\r\n" +
    //            "<</Type /Pages/Kids [3 0 R 5 0 R]/Count 2>>\r\n" +
    //            "endobj\r\n" +
    //            "5 0 obj\r\n" +
    //            "<</Type /Page/Parent 2 0 R/Resources <<>>/MediaBox [0 0 200 200]>>\r\n" +
    //            "endobj\r\n" +
    //            "xref\r\n" +
    //            "2 1\r\n" +
    //            "0000000406 00000 n\r\n" +
    //            "5 1\r\n" +
    //            "0000000468 00000 n\r\n" +
    //            "trailer\r\n" +
    //            "<</Size 5/Prev 184/Root 1 0 R/ID [<2045e2246d17437290c929c74954eb23> <2045e2246d17437290c929c74954eb23>]>>\r\n" +
    //            "startxref\r\n" +
    //            "553\r\n" +
    //            "%%EOF";

    //        output.Should().Be(expectedOutput);
    //    
}
