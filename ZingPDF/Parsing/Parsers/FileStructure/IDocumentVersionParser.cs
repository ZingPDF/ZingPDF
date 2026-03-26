using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Parsing.Parsers.FileStructure;

public interface IDocumentVersionParser
{
    Task<VersionInformation> ParseLatestAsync(Stream pdfInputStream);
    Task<VersionInformation> ParseAtAsync(Stream pdfInputStream, int xrefOffset);
    Task<List<VersionInformation>> ParseAsync(Stream pdfInputStream);
}
