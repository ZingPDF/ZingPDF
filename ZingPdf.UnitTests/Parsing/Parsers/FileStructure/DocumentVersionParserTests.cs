using FluentAssertions;
using Xunit;
using ZingPDF.UnitTests.TestFiles;

namespace ZingPDF.Parsing.Parsers.FileStructure;

public class DocumentVersionParserTests
{
    [Theory]
    [InlineData(Files.ComboboxForm, 1)]
    [InlineData(Files.ComplexForm, 2)]
    [InlineData(Files.Form, 5)]
    [InlineData(Files.Ghostscript, 1)]
    [InlineData(Files.MikeyPortfolio, 2)]
    [InlineData(Files.Minimal1, 1)]
    [InlineData(Files.Minimal2, 2)]
    [InlineData(Files.Minimal3, 2)]
    [InlineData(Files.Test, 1)]
    public async Task ParseDocumentVersions_CorrectCount(string filePath, int expectedVersionCount)
    {
        var stream = new MemoryStream(Files.ConcurrentRead(filePath));
        var pdfContext = new PdfContext(stream);

        var versions = await pdfContext.Parser.DocumentVersions.ParseDocumentVersionsAsync(stream);

        versions.Count.Should().Be(expectedVersionCount);
    }
}
