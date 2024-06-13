namespace ZingPDF.Parsing;

public class EditablePdf : IPdf
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

    #region IPdf

    public Task<int> GetPageCountAsync()
    {
        throw new NotImplementedException();
    }

    #endregion
}
