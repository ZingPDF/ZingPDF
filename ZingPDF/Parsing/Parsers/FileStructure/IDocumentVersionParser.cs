using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Parsing.Parsers.FileStructure;

public interface IDocumentVersionParser
{
    ValueTask<VersionInformation> ParseLatestAsync(Stream pdfInputStream);
    ValueTask<VersionInformation> ParseAtAsync(Stream pdfInputStream, int xrefOffset);
    ValueTask<List<VersionInformation>> ParseAsync(Stream pdfInputStream);
}
