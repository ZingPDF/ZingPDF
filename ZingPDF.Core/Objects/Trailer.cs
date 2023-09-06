using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// PDF 32000-1:2008 7.5.5
    /// </summary>
    internal class Trailer : PdfObject
    {
        private readonly IndirectObject _documentCatalog;
        private readonly CrossReferenceTable _xrefTable;
        private readonly int _objectCount;

        public Trailer(IndirectObject documentCatalog, CrossReferenceTable xrefTable, int objectCount)
        {
            _documentCatalog = documentCatalog ?? throw new ArgumentNullException(nameof(documentCatalog));
            _xrefTable = xrefTable ?? throw new ArgumentNullException(nameof(xrefTable));
            _objectCount = objectCount;
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteNewLineAsync();
            await stream.WriteTextAsync(Constants.Trailer);
            await stream.WriteNewLineAsync();

            var trailerDictionary = new Dictionary<Name, PdfObject>
            {
                { "Size", new Integer(_objectCount) },
                { "Root", _documentCatalog.Id }
            };

            if (false) // TODO: this is for when there are more than one cross reference table
            {
                //trailerDictionary.Add("Prev", new IndirectObjectReference(0, 0));
            }

            if (false) // TODO: this is for when the document is encrypted
            {
                //trailerDictionary.Add("Encrypt", new Dictionary(new Dictionary<Name, PdfObject> { }));
            }

            if (false) // TODO: this is for when there is an info dictionary
            {
                //trailerDictionary.Add("Info", new IndirectObjectReference(0, 0));
            }

            if (false) // TODO: this is required if encrypted, optional otherwise
            {
                //trailerDictionary.Add("ID", new Primitives.Array(new PdfObject[] { }));
            }

            await new Dictionary(trailerDictionary).WriteAsync(stream);

            // Cross-reference table location
            await stream.WriteNewLineAsync();
            await stream.WriteTextAsync(Constants.StartXref);
            await stream.WriteNewLineAsync();

            await stream.WriteLongAsync(_xrefTable.ByteOffset!.Value);
            await stream.WriteNewLineAsync();

            await stream.WriteTextAsync(Constants.Eof);
        }
    }
}
