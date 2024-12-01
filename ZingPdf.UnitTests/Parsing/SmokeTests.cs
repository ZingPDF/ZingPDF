using Xunit;
using ZingPDF.UnitTests.TestFiles;

namespace ZingPDF.Parsing;

public class SmokeTests
{
    [Theory]
    [InlineData("testfiles/pdf/combobox-form.pdf")]
    [InlineData("testfiles/pdf/complex-form.pdf")]
    [InlineData("testfiles/pdf/form.pdf")]    
    [InlineData("testfiles/pdf/Ghostscript.pdf")]
    [InlineData("testfiles/pdf/MikeyFlemingFreelance_Folio.pdf")]
    [InlineData("testfiles/pdf/minimal.pdf")]
    [InlineData("testfiles/pdf/minimal2.pdf")]
    [InlineData("testfiles/pdf/minimal3.pdf")]
    [InlineData("testfiles/pdf/test.pdf")]
    public async Task Parse(string filePath)
    {
        var pdf = await PdfParser.OpenAsync(new MemoryStream(Files.ConcurrentRead(filePath)));

        await pdf.GetPageCountAsync();
    }
}