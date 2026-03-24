using ZingPDF.Elements;
using ZingPDF.Elements.Drawing.Text.Extraction;
using ZingPDF.Elements.Forms;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

/// <summary>
/// Represents a loaded or newly created PDF document.
/// </summary>
public interface IPdf
{
    /// <summary>
    /// Gets the underlying source stream for the PDF.
    /// </summary>
    Stream Data { get; }

    /// <summary>
    /// Gets the low-level PDF object collection for advanced scenarios.
    /// </summary>
    IPdfObjectCollection Objects { get; }

    /// <summary>
    /// Unlocks the PDF for reading and/or writing. The password can be a user password or an owner password.
    /// </summary>
    Task AuthenticateAsync(string password);

    /// <summary>
    /// Gets every page indirect object in page order.
    /// </summary>
    Task<IList<IndirectObject>> GetAllPagesAsync();

    /// <summary>
    /// Gets a page by its 1-based page number.
    /// </summary>
    Task<Page> GetPageAsync(int pageNumber);

    /// <summary>
    /// Gets the number of pages in the document.
    /// </summary>
    Task<int> GetPageCountAsync();

    /// <summary>
    /// Gets the document form wrapper if the PDF contains an AcroForm.
    /// </summary>
    Task<Form?> GetFormAsync();

    /// <summary>
    /// Gets the editable document metadata backed by the trailer Info dictionary.
    /// </summary>
    /// <remarks>
    /// Changes are persisted when <see cref="SaveAsync(Stream)"/> is called.
    /// Saving also stamps the metadata producer as <c>ZingPDF</c> and refreshes the modification date.
    /// </remarks>
    Task<PdfMetadata> GetMetadataAsync();

    /// <summary>
    /// Appends a new page to the end of the document.
    /// </summary>
    Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null);

    /// <summary>
    /// Inserts a new page before the specified 1-based page number.
    /// </summary>
    Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null);

    /// <summary>
    /// Deletes a page by its 1-based page number.
    /// </summary>
    Task DeletePageAsync(int pageNumber);

    /// <summary>
    /// Sets the rotation for every page in the document.
    /// </summary>
    Task SetRotationAsync(Rotation rotation);

    /// <summary>
    /// Extracts text from the document.
    /// </summary>
    Task<IEnumerable<ExtractedText>> ExtractTextAsync();

    /// <summary>
    /// Adds a simple text watermark to each page.
    /// </summary>
    Task AddWatermarkAsync(string text);

    /// <summary>
    /// Compresses eligible streams in the document.
    /// </summary>
    /// <param name="dpi">Reserved for future image downsampling behavior. Must be greater than zero.</param>
    /// <param name="quality">JPEG recompression quality from 1 to 100.</param>
    void Compress(int dpi, int quality);

    /// <summary>
    /// Removes compression filters from all objects in the PDF.
    /// </summary>
    /// <remarks>
    /// This applies an incremental update to the PDF with all objects decompressed. This can add significant size to the PDF.
    /// Typically, this is used to make the source code of a PDF more readable.
    /// </remarks>
    Task DecompressAsync();

    /// <summary>
    /// Saves the PDF using standard password protection.
    /// </summary>
    /// <remarks>
    /// If the source PDF is already encrypted, authenticate it first so the document can be rewritten.
    /// The current implementation writes Standard security handler encryption using RC4 (V=2, R=3).
    /// </remarks>
    Task EncryptAsync(string userPassword, string? ownerPassword = null);

    /// <summary>
    /// Authenticates with the supplied password and saves the PDF without encryption.
    /// </summary>
    Task DecryptAsync(string password);

    /// <summary>
    /// Appends the pages from another PDF stream to the current document.
    /// </summary>
    Task AppendPdfAsync(Stream stream);

    /// <summary>
    /// Saves the document by writing an incremental update.
    /// </summary>
    /// <remarks>
    /// When saving to a different stream, the output stream must be empty, writable, and seekable.
    /// </remarks>
    Task SaveAsync(Stream outputStream);
}
