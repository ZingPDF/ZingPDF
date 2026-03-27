//using FluentAssertions;
//using Xunit;

//namespace ZingPDF.Parsing.Parsers.FileStructure;

//public class DocumentVersionParserTests
//{
//    [Theory]
//    [InlineData(Files.ComboboxForm, 1)]
//    [InlineData(Files.ComplexForm, 2)]
//    [InlineData(Files.Form, 5)]
//    [InlineData(Files.GeneratedImageHeavy, 1)]
//    [InlineData(Files.GeneratedIncrementalHistory, 2)]
//    [InlineData(Files.Minimal1, 1)]
//    [InlineData(Files.Minimal2, 2)]
//    [InlineData(Files.Minimal3, 2)]
//    [InlineData(Files.Test, 1)]
//    public async Task ParseDocumentVersions_CorrectCount(string filePath, int expectedVersionCount)
//    {
//        var stream = new MemoryStream(Files.ConcurrentRead(filePath));
//        var pdfObjects = new PdfContext(stream);

//        var versions = await pdfObjects.Parser.DocumentVersions.ParseDocumentVersionsAsync(stream);

//        versions.Count.Should().Be(expectedVersionCount);
//    }
//}
// TODO: these shouldn't use real files, convert to proper unit tests
