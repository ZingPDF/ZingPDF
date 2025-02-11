using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.IncrementalUpdates
{
    public class IncrementalUpdate : PdfObject
    {
        private readonly DocumentVersion _delta;
        private readonly IEnumerable<IndirectObject> _newOrUpdatedObjects;
        private readonly IncrementalUpdateOptions _options;

        public IncrementalUpdate(
            DocumentVersion delta,
            IEnumerable<IndirectObject> newOrUpdatedObjects,
            IncrementalUpdateOptions? options = null
            )
        {
            ArgumentNullException.ThrowIfNull(delta, nameof(delta));
            ArgumentNullException.ThrowIfNull(newOrUpdatedObjects, nameof(newOrUpdatedObjects));

            _delta = delta;
            _newOrUpdatedObjects = newOrUpdatedObjects;
            _options = options ?? IncrementalUpdateOptions.Default;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteNewLineAsync();

            // Write all updated and new objects to the output stream
            foreach (var entry in _newOrUpdatedObjects)
            {
                await entry.WriteAsync(stream);
            }

            // For now, the IndirectObjectDictionary generates a delta with xref table and trailer.
            // TODO: add support for rendering xrefs as stream (left legacy version commented out below)
            await _delta.CrossReferenceTable.WriteAsync(stream);

            await _delta.Trailer.WriteAsync(stream);
        }

        //if (_options.RenderCrossReferencesAsStream)
        //{
        //    // When rendering as an xref stream, the stream itself needs to be present as a reference within itself.
        //    // To acheive this, create a dummy indirect object, with its fake byte offset set to the current stream position.
        //    // Then build the xref stream, including the dummy object.
        //    // As long as the next thing written is the xref stream, its byte offset will be correct.

        //    IndirectObjectId newObjectId = _indirectObjectDictionary.GetNextFreeId();

        //    var xrefStreamIndirectObject = new DummyIndirectObject(newObjectId, stream.Position);

        //    _indirectObjectDictionary.NewObjects.Add(xrefStreamIndirectObject);

        //    List<CrossReferenceSection> xrefSections = xrefGenerator.Generate(
        //        _indirectObjectDictionary.NewOrUpdatedObjects,
        //        _indirectObjectDictionary.DeletedObjects
        //        );

        //    // +1 because the new xref stream should be included in the count
        //    size++;

        //    var xrefStream = new CrossReferenceStreamFactory(
        //        xrefSections,
        //        size,
        //        _sourcePdf.TrailerDictionary.Root,
        //        prev,
        //        _sourcePdf.TrailerDictionary.Encrypt,
        //        _sourcePdf.TrailerDictionary.Info,
        //        fileIdentifier
        //        )
        //        .Create();

        //    xrefStreamIndirectObject.SetObject(xrefStream);

        //    await xrefStreamIndirectObject.WriteAsync(stream);

        //    await new Keyword(Constants.StartXref).WriteAsync(stream);
        //    await stream.WriteNewLineAsync();

        //    await new Integer(xrefStreamIndirectObject.ByteOffset!.Value).WriteAsync(stream);
        //    await stream.WriteNewLineAsync();

        //    await new Keyword(Constants.Eof).WriteAsync(stream);
        //}

        // TODO: account for the use of features which should increase the pdf version

        // TODO: do we need to amend metadata to change PDF Producer?

        //private class DummyIndirectObject : IndirectObject
        //{
        //    public DummyIndirectObject(IndirectObjectId id, long byteOffset)
        //        : base(id, new DummyObject())
        //    {
        //        ByteOffset = byteOffset;
        //    }

        //    public void SetObject(IPdfObject pdfObject) => Object = pdfObject; 

        //    private class DummyObject : IPdfObject
        //    {
        //        public long? ByteOffset => throw new NotImplementedException();
        //        public bool Written => throw new NotImplementedException();

        //        public Task WriteAsync(Stream stream)
        //        {
        //            throw new NotImplementedException();
        //        }
        //    }
        //}
    }
}
