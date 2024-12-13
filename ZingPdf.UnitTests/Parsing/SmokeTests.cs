using Xunit;
using ZingPDF.UnitTests.TestFiles;

namespace ZingPDF.Parsing;

public class SmokeTests
{
    [Theory]
    [InlineData(Files.ComboboxForm)]
    [InlineData(Files.ComplexForm)]
    [InlineData(Files.Form)]    
    [InlineData(Files.Ghostscript)]
    [InlineData(Files.MikeyPortfolio)]
    [InlineData(Files.Minimal1)]
    [InlineData(Files.Minimal2)]
    [InlineData(Files.Minimal3)]
    [InlineData(Files.Test)]
    public async Task Parse(string filePath)
    {
        var pdf = await PdfParser.OpenAsync(new MemoryStream(Files.ConcurrentRead(filePath)));

        await pdf.GetPageCountAsync();
    }
}