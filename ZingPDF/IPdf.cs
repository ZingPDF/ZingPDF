using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

public interface IPdf
{
    Task<IList<IndirectObject>> GetAllPagesAsync();
    Task<Page> GetPageAsync(int pageNumber);
    Task<int> GetPageCountAsync();

    Form? GetForm();

    Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null);
    Task DeletePageAsync(int pageNumber);
    Task SetRotationAsync(Rotation rotation);

    void AddWatermark();

    void Compress(int dpi, int quality);

    /// <summary>
    /// Removes compression filters from all objects in the PDF.
    /// </summary>
    /// <remarks>
    /// This applies an incremental update to the PDF with all objects decompressed. This can add significant size to the PDF.
    /// Typically, this is used to make the source code of a PDF more readable.
    /// </remarks>
    Task DecompressAsync();

    void Encrypt();
    void Decrypt();

    void Sign();
    Task AppendPdfAsync(Stream stream);

    Task SaveAsync(Stream outputStream, PdfSaveOptions? saveOptions);

    IPdfEditor IndirectObjects { get; }
    PageTree PageTree { get; }
}
