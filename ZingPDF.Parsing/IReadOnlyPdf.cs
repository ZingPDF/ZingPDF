using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.FileStructure.Trailer;

namespace ZingPDF.Parsing;

public interface IReadOnlyPdf
{
    ReadOnlyIndirectObjectDictionary IndirectObjects { get; }
    DocumentCatalogDictionary DocumentCatalog { get; }
    ITrailerDictionary TrailerDictionary { get; }
}
