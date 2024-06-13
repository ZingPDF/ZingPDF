using ZingPDF.Drawing;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;

namespace ZingPDF.Parsing;

public class EditablePdf : IEditablePdf
{
    private readonly ReadOnlyPdf _sourcePdf;

    /// <summary>
    /// Private constructor for creating an <see cref="EditablePdf"/> from a <see cref="ReadOnlyPdf"/>.
    /// </summary>
    private EditablePdf(ReadOnlyPdf sourcePdf)
    {
        _sourcePdf = sourcePdf ?? throw new ArgumentNullException(nameof(sourcePdf));
    }

    /// <summary>
    /// Load a PDF from a stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> which contains the PDF data.</param>
    /// <returns>A <see cref="Pdf"/> instance.</returns>
    /// <example>
    /// <![CDATA[
    /// using var inputFileStream = new FileStream("example.pdf", FileMode.Open);
    /// 
    /// var pdf = await EditablePdf.LoadAsync(inputFileStream);
    /// ]]>
    /// </example>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<EditablePdf> LoadAsync(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        }

        return new(await ReadOnlyPdf.LoadAsync(stream));
    }

    public void AddWatermark()
    {
        throw new NotImplementedException();
    }

    public Task AppendPageAsync(Page.PageCreationOptions? pageCreationOptions)
    {
        throw new NotImplementedException();
    }

    public void AppendPdf(Stream stream)
    {
        throw new NotImplementedException();
    }

    public void CompleteForm(IDictionary<string, string> values)
    {
        throw new NotImplementedException();
    }

    public void Compress(int dpi, int quality)
    {
        throw new NotImplementedException();
    }

    public void Decrypt()
    {
        throw new NotImplementedException();
    }

    public Task DeletePageAsync(int pageNumber)
    {
        throw new NotImplementedException();
    }

    public void Draw(int pageNumber, IEnumerable<Drawing.Path> paths, IEnumerable<Text> text, IEnumerable<Image> imageOperations, CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp)
    {
        throw new NotImplementedException();
    }

    public void Encrypt()
    {
        throw new NotImplementedException();
    }

    public IDictionary<string, string?> GetFields()
    {
        throw new NotImplementedException();
    }

    public Task<Page> GetPageAsync(int pageNumber)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetPageCountAsync()
    {
        throw new NotImplementedException();
    }

    public Task InsertPageAsync(int pageNumber, Page.PageCreationOptions? pageCreationOptions)
    {
        // get page at number
        // get parent page tree node
        // add new page indirect object
        // add new page ref to kids property
        // increment page count
        // - this involves recursively updating multiple nodes in page tree

        throw new NotImplementedException();
    }

    public Task SetPageRotationAsync(int pageNumber, Rotation rotation)
    {
        throw new NotImplementedException();
    }

    public void Sign()
    {
        throw new NotImplementedException();
    }
}
