using FluentAssertions;
using Xunit;
using ZingPDF.Extensions;
using ZingPDF.Parsing;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.UnitTests.TestFiles;

namespace ZingPDF;

public class PdfTests
{
    [Fact]
    public async Task AppendPage_PageCount()
    {
        var pdf = await PdfParser.OpenAsync(new MemoryStream(Files.ConcurrentRead("testfiles/pdf/minimal.pdf")));

        var pageCount1 = await pdf.GetPageCountAsync();

        _ = await pdf.AppendPageAsync();

        var pageCount2 = await pdf.GetPageCountAsync();

        pageCount2.Should().Be(pageCount1 + 1);
    }

    [Fact]
    public async Task AppendPage_CanRetrieveAfterAdding()
    {
        var pdf = await PdfParser.OpenAsync(new MemoryStream(Files.ConcurrentRead("testfiles/pdf/minimal.pdf")));

        _ = await pdf.AppendPageAsync();

        var addedPage = await pdf.GetPageAsync(2);

        addedPage.Should().NotBeNull();
    }

    [Fact]
    public async Task InsertPage_PageCount()
    {
        var pdf = await PdfParser.OpenAsync(new MemoryStream(Files.ConcurrentRead("testfiles/pdf/minimal.pdf")));

        var pageCount1 = await pdf.GetPageCountAsync();

        _ = await pdf.InsertPageAsync(1, options => options.MediaBox = Rectangle.FromSize(100, 100));

        var pageCount2 = await pdf.GetPageCountAsync();

        pageCount2.Should().Be(pageCount1 + 1);
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
