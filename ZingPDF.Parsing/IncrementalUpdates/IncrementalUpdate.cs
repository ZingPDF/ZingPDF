using ZingPDF.Extensions;
using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Parsing.IncrementalUpdates
{
    internal class IncrementalUpdate : PdfObject
    {
        private readonly IndirectObjectManager _indirectObjectManager;

        private readonly ReadOnlyPdf _sourcePdf;
        private readonly IncrementalUpdateOptions _options;

        public IncrementalUpdate(
            ReadOnlyPdf sourcePdf,
            IndirectObjectManager indirectObjectManager,
            IncrementalUpdateOptions? options = null
            )
        {
            _sourcePdf = sourcePdf ?? throw new ArgumentNullException(nameof(sourcePdf));
            _indirectObjectManager = indirectObjectManager ?? throw new ArgumentNullException(nameof(indirectObjectManager));
            _options = options ?? IncrementalUpdateOptions.Default;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            if (_indirectObjectManager.NewOrUpdatedObjects.Count == 0 && _indirectObjectManager.DeletedObjects.Count == 0)
            {
                return;
            }

            var xrefGenerator = new CrossReferenceGenerator();

            await stream.WriteNewLineAsync();

            // Write all updated and new objects to the output stream
            foreach (var entry in _indirectObjectManager.NewOrUpdatedObjects)
            {
                await entry.WriteAsync(stream);
            }

            var size = _sourcePdf.TrailerDictionary.Size + _indirectObjectManager.NewObjects.Count;

            // The prev value points to the previous latest xref table or stream.
            // If the current PDF has a trailer, prev should be the same as the current startxref value.
            // If the current PDF instead uses an xref stream dictionary, prev is going to be the offset of the stream dictionary
            long prev = _sourcePdf.Trailer?.XrefTableByteOffset ?? _sourcePdf.TrailerDictionary.ByteOffset!.Value;

            if (_options.RenderCrossReferencesAsStream)
            {
                // When rendering as an xref stream, the stream itself needs to be present as a reference within itself.
                // To acheive this, create a dummy indirect object, with its fake byte offset set to the current stream position.
                // Then build the xref stream, including the dummy object.
                // As long as the next thing written is the xref stream, its byte offset will be correct.

                IndirectObjectId newObjectId = _indirectObjectManager.GetNextFreeId();

                var xrefStreamIndirectObject = new DummyIndirectObject(newObjectId, stream.Position);

                _indirectObjectManager.NewObjects.Add(xrefStreamIndirectObject);

                List<CrossReferenceSection> xrefSections = xrefGenerator.Generate(
                    _indirectObjectManager.NewOrUpdatedObjects,
                    _indirectObjectManager.DeletedObjects
                    );

                // +1 because the new xref stream should be included in the count
                size++;

                var xrefStream = new CrossReferenceStream(
                    xrefSections,
                    null,
                    //new[] { new FlateDecodeFilter(filterParams: null) },
                    //new[] { new ASCIIHexDecodeFilter() },
                    size,
                    prev,
                    _sourcePdf.TrailerDictionary.Root,
                    _sourcePdf.TrailerDictionary.Encrypt,
                    _sourcePdf.TrailerDictionary.Info,
                    _sourcePdf.TrailerDictionary.ID
                    );

                xrefStreamIndirectObject.Children.Clear();
                xrefStreamIndirectObject.Children.Add(xrefStream);

                await xrefStreamIndirectObject.WriteAsync(stream);

                await new Keyword(Constants.StartXref).WriteAsync(stream);
                await stream.WriteNewLineAsync();

                await new Integer(xrefStreamIndirectObject.ByteOffset!.Value).WriteAsync(stream);
                await stream.WriteNewLineAsync();

                await new Keyword(Constants.Eof).WriteAsync(stream);
            }
            else
            {
                List<CrossReferenceSection> xrefSections = xrefGenerator.Generate(
                    _indirectObjectManager.NewOrUpdatedObjects,
                    _indirectObjectManager.DeletedObjects
                    );

                var xrefTable = new CrossReferenceTable(xrefSections);
                await xrefTable.WriteAsync(stream);

                var trailer = new Trailer(
                    TrailerDictionary.CreateNew(
                        size,
                        prev,
                        _sourcePdf.TrailerDictionary.Root,
                        _sourcePdf.TrailerDictionary.Encrypt,
                        _sourcePdf.TrailerDictionary.Info,
                        _sourcePdf.TrailerDictionary.ID
                        ),
                    xrefTable.ByteOffset!.Value
                    );

                await trailer.WriteAsync(stream);
            }

            // TODO: account for the use of features which should increase the pdf version

            // TODO: do we need to amend metadata to change PDF Producer?
        }

        private class DummyIndirectObject : IndirectObject
        {
            public DummyIndirectObject(IndirectObjectId id, long byteOffset)
                : base(id)
            {
                ByteOffset = byteOffset;
            }
        }
    }
}
