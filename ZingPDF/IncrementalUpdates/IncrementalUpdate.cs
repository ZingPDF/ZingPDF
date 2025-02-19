using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.IncrementalUpdates
{
    public class IncrementalUpdate : PdfObject
    {
        private readonly IEnumerable<IndirectObject> _newObjects;
        private readonly IEnumerable<IndirectObject> _updatedObjects;
        private readonly IEnumerable<IndirectObjectId> _deletedObjects;
        private readonly Trailer? _existingTrailer;
        private readonly StreamObject<CrossReferenceStreamDictionary>? _existingXrefStream;

        private readonly IncrementalUpdateOptions _options;

        public IncrementalUpdate(
            IEnumerable<IndirectObject> newObjects,
            IEnumerable<IndirectObject> updatedObjects,
            IEnumerable<IndirectObjectId> deletedObjects,
            Trailer? existingTrailer,
            StreamObject<CrossReferenceStreamDictionary>? existingXrefStream,
            IncrementalUpdateOptions? options = null
            )
        {
            ArgumentNullException.ThrowIfNull(newObjects, nameof(newObjects));
            ArgumentNullException.ThrowIfNull(updatedObjects, nameof(updatedObjects));
            ArgumentNullException.ThrowIfNull(deletedObjects, nameof(deletedObjects));

            _newObjects = newObjects;
            _updatedObjects = updatedObjects;
            _deletedObjects = deletedObjects;
            _existingTrailer = existingTrailer;
            _existingXrefStream = existingXrefStream;
            _options = options ?? IncrementalUpdateOptions.Default;
        }

        public HashSet<IndirectObject> NewOrUpdatedObjects
        {
            get
            {
                var objects = new HashSet<IndirectObject>(_updatedObjects);

                foreach (var obj in _newObjects)
                {
                    objects.Add(obj);
                }

                return objects;
            }
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteNewLineAsync();

            // Write all updated and new objects to the output stream
            foreach (var entry in NewOrUpdatedObjects)
            {
                await entry.WriteAsync(stream);
            }

            var crossReferenceSections = CrossReferenceGenerator.Generate(NewOrUpdatedObjects, _deletedObjects);
            var xrefTable = new CrossReferenceTable(crossReferenceSections);

            // For now, the IndirectObjectDictionary generates a delta with xref table and trailer.
            // TODO: add support for rendering xrefs as stream (left legacy version commented out below)
            await xrefTable.WriteAsync(stream);

            // The prev value points to the previous latest xref table or stream.
            // If the current PDF has a trailer, prev should be the same as the current startxref value.
            // If the current PDF instead uses an xref stream dictionary, prev is going to be the offset of the stream dictionary
            long prev = _existingTrailer?.XrefTableByteOffset ?? _existingXrefStream!.ByteOffset!.Value;

            var existingTrailerDictionary = _existingTrailer?.Dictionary ?? (ITrailerDictionary)_existingXrefStream!.Dictionary;

            // Build file identifier
            var originalId = existingTrailerDictionary.ID?[0] ?? HexadecimalString.FromBytes(Guid.NewGuid().ToByteArray());
            var updateId = HexadecimalString.FromBytes(Guid.NewGuid().ToByteArray());
            var fileIdentifier = new ArrayObject([originalId, updateId]);

            var trailer = new Trailer(
                TrailerDictionary.CreateNew(
                    existingTrailerDictionary.Size + _newObjects.Count(),
                    prev,
                    existingTrailerDictionary.Root, // TODO: figure out how best to handle this if it can be null
                    existingTrailerDictionary.Encrypt,
                    existingTrailerDictionary.Info,
                    fileIdentifier
                    ),
                xrefTable.ByteOffset!.Value
                );

            await trailer.WriteAsync(stream);
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
