using Xunit;
using ZingPDF.Tests.Smoke.TestFiles;

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
    [InlineData(Files.Encrypted)]
    public async Task Parse(string filePath)
    {
        var pdf = Pdf.Load(new MemoryStream(Files.ConcurrentRead(filePath)));

        await pdf.GetPageCountAsync();
    }
}