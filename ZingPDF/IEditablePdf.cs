using ZingPDF.Elements;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure.PageTree;

namespace ZingPDF;

public interface IEditablePdf : IPdf
{
    Task<Page> AppendPageAsync(PageDictionary.PageCreationOptions? pageCreationOptions);
    Task<Page> InsertPageAsync(int pageNumber, PageDictionary.PageCreationOptions? pageCreationOptions);
    Task DeletePageAsync(int pageNumber);
    Task SetRotationAsync(Rotation rotation);

    Task CompleteFormAsync(IDictionary<string, string> formValues);

    void AddWatermark();
    void Compress(int dpi, int quality);
    void Encrypt();
    void Decrypt();
    void Sign();
    void AppendPdf(Stream stream);
}
