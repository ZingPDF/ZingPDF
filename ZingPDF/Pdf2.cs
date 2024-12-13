using ZingPDF.Elements;
using ZingPDF.Elements.Forms;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers.FileStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF;

internal class Pdf2 : IPdf2
{
    private readonly Stream _pdfInputStream;
    private readonly IIndirectObjectDictionary _indirectObjects;

    private readonly PageTree _pageTree;

    private Pdf2(
        Stream pdfInputStream,
        List<DocumentVersion> versions
        )
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));
        ArgumentNullException.ThrowIfNull(versions, nameof(versions));

        _pdfInputStream = pdfInputStream;
        _indirectObjects = new IndirectObjectDictionary(pdfInputStream, versions);

        _pageTree = new PageTree(_indirectObjects, );
    }

    public static async Task<Pdf2> LoadAsync(Stream pdfInputStream)
    {
        ArgumentNullException.ThrowIfNull(pdfInputStream, nameof(pdfInputStream));

        if (!pdfInputStream.CanSeek)
        {
            throw new ArgumentException("Provided stream must be seekable");
        }

        var documentVersions = await DocumentVersionParser.ParseDocumentVersionsAsync(pdfInputStream);

        return new Pdf2(pdfInputStream, documentVersions);
    }

    public Task<IList<IndirectObject>> GetAllPagesAsync()
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

    public Form? GetForm()
    {
        throw new NotImplementedException();
    }

    public Task<Page> AppendPageAsync(Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        throw new NotImplementedException();
    }

    public Task<Page> InsertPageAsync(int pageNumber, Action<PageDictionary.PageCreationOptions>? configureOptions = null)
    {
        throw new NotImplementedException();
    }

    public Task DeletePageAsync(int pageNumber)
    {
        throw new NotImplementedException();
    }

    public Task SetRotationAsync(Rotation rotation)
    {
        throw new NotImplementedException();
    }

    public void AddWatermark()
    {
        throw new NotImplementedException();
    }

    public void Compress(int dpi, int quality)
    {
        throw new NotImplementedException();
    }

    public void Encrypt()
    {
        throw new NotImplementedException();
    }

    public void Decrypt()
    {
        throw new NotImplementedException();
    }

    public void Sign()
    {
        throw new NotImplementedException();
    }

    public Task AppendPdfAsync(Stream stream)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Stream stream, PdfSaveOptions? saveOptions)
    {
        throw new NotImplementedException();
    }
}

// Commenting this while I test if it works with the original parsers
//pdfInputStream.Position = 0;
//using var reader = new StreamReader(pdfInputStream, Encoding.ASCII, leaveOpen: true);

//var content = await GetFirstKBOfFileAsync(reader);

//var linearizationKeyPosition = FindLinearizationKey(content);

//var fileHasLinearizationDictionary = linearizationKeyPosition != null;
//if (fileHasLinearizationDictionary)
//{
//    var objStartIndex = FindStartOfCurrentObject(content, linearizationKeyPosition!.Value);

//    var dictionaryContent = GetDictionaryAsString(content, objStartIndex);

//    var stringDictionary = ParseToDictionaryOfStrings(dictionaryContent);

//    linearizationDictionary = ConvertToTypedLinearizationDictionary(stringDictionary);

//    var dictEnd = objStartIndex + dictionaryContent.Length;
//    var objEndIndex = FindEndOfLinearizationDictionaryObject(content, dictEnd);

//    pdfInputStream.Position = objEndIndex;
//}

//var items = await Parser.PdfObjectGroups.ParseAsync(new SubStream(pdfInputStream, 0, 1024), HoneyTrapIndirectObjectDictionary.Instance);

//var linearizationDictionaryObject = (IndirectObject?)items.Objects
//    .FirstOrDefault(x => x is IndirectObject o && o.Object is LinearizationParameterDictionary);

//linearizationDictionary = linearizationDictionaryObject?.Object as LinearizationParameterDictionary;