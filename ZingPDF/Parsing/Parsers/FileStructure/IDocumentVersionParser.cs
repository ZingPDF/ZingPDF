using ZingPDF.IncrementalUpdates;

namespace ZingPDF.Parsing.Parsers.FileStructure;

public interface IDocumentVersionParser
{
    Task<List<VersionInformation>> ParseAsync(Stream pdfInputStream);
}
