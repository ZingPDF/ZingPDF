using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.IncrementalUpdates
{
    internal class IncrementalUpdate : PdfObject
    {
        private readonly EditablePdfNavigator _pdfNavigator;
        private readonly IncrementalUpdateOptions _options;

        public IncrementalUpdate(
            EditablePdfNavigator pdfNavigator,
            IncrementalUpdateOptions? options = null
            )
        {
            _pdfNavigator = pdfNavigator ?? throw new ArgumentNullException(nameof(pdfNavigator));

            _options = options ?? IncrementalUpdateOptions.Default;
        }

        public List<IndirectObject> NewObjects { get; } = [];
        public Dictionary<IndirectObjectId, IndirectObject> UpdatedObjects { get; } = [];
        public List<IndirectObjectId> DeletedObjects { get; } = [];

        public List<IndirectObject> NewOrUpdatedObjects { get => UpdatedObjects.Values.Concat(NewObjects).ToList(); }

        public Trailer? Trailer { get; private set; }
        public ITrailerDictionary? TrailerDictionary { get; private set; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            var xrefGenerator = new CrossReferenceGenerator();

            await stream.WriteNewLineAsync();

            // Write all updated and new objects to the output stream
            foreach (var entry in UpdatedObjects.Values.Concat(NewObjects))
            {
                await entry.WriteAsync(stream);
            }

            TrailerDictionary = await _pdfNavigator.GetRootTrailerDictionaryAsync();

            var size = TrailerDictionary.Size + NewObjects.Count;

            if (_options.RenderCrossReferencesAsStream)
            {
                // When rendering as an xref stream, the stream itself needs to be present as a reference within itself.
                // To acheive this, create a dummy indirect object, with its fake byte offset set to the current stream position.
                // Then build the xref stream, including the dummy object.
                // As long as the next thing written is the xref stream, its byte offset will be correct.

                IndirectObjectId newObjectId = await _pdfNavigator.GetFreeIndexAsync();

                var xrefStreamIndirectObject = new DummyIndirectObject(newObjectId, stream.Position);

                NewObjects.Add(xrefStreamIndirectObject);

                List<CrossReferenceSection> xrefSections = xrefGenerator.Generate(NewOrUpdatedObjects, DeletedObjects);

                // +1 because the new xref stream should be included in the count
                size++;

                var xrefStreamDict = TrailerDictionary as CrossReferenceStreamDictionary
                    ?? throw new InvalidOperationException("Internal Error: {59D30CD9-D2DB-4418-B59E-033538307C68}");

                var prev = TrailerDictionary.ByteOffset;

                var xrefStream = new CrossReferenceStream(
                    xrefSections,
                    null,
                    //new[] { new FlateDecodeFilter(filterParams: null) },
                    //new[] { new ASCIIHexDecodeFilter() },
                    size,
                    prev,
                    xrefStreamDict.Root,
                    xrefStreamDict.Encrypt,
                    xrefStreamDict.Info,
                    xrefStreamDict.ID
                    );

                xrefStreamIndirectObject.Children.Clear();
                xrefStreamIndirectObject.Children.Add(xrefStream);

                await xrefStreamIndirectObject.WriteAsync(stream);

                await new Keyword(Constants.StartXref).WriteAsync(stream);
                await stream.WriteNewLineAsync();

                await new Integer(stream.Length + xrefStreamIndirectObject.ByteOffset!.Value).WriteAsync(stream);
                await stream.WriteNewLineAsync();

                await new Keyword(Constants.Eof).WriteAsync(stream);
            }
            else
            {
                List<CrossReferenceSection> xrefSections = xrefGenerator.Generate(NewOrUpdatedObjects, DeletedObjects);

                var xrefTable = new CrossReferenceTable(xrefSections);
                await xrefTable.WriteAsync(stream);

                var latestTrailer = (await _pdfNavigator.GetRootTrailerAsync())!;
                var prev = latestTrailer.XrefTableByteOffset;

                Trailer = new Trailer(
                    Objects.ObjectGroups.Trailer.TrailerDictionary.CreateNew(
                        size,
                        prev,
                        latestTrailer.Dictionary.Root,
                        latestTrailer.Dictionary.Encrypt,
                        latestTrailer.Dictionary.Info,
                        latestTrailer.Dictionary.ID
                        ),
                    xrefTable.ByteOffset!.Value
                    );

                await Trailer.WriteAsync(stream);
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
