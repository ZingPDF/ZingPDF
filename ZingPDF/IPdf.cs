using ZingPDF.ObjectModel.DocumentStructure;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    IIndirectObjectDictionary IndirectObjects { get; }
    Trailer? Trailer { get; }
    ITrailerDictionary TrailerDictionary { get; }
    DocumentCatalogDictionary DocumentCatalog { get; }

    Task<IndirectObject> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Task SaveAsync(Stream stream, PdfSaveOptions? saveOptions);
}
