using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Filters;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences.CrossReferenceStreams;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    // TODO: unit tests are vital for this class
    internal class IncrementalUpdateManager
    {
        private readonly List<IndirectObject> _newObjects = new();
        private readonly Dictionary<IndirectObjectId, IndirectObject> _updatedObjects = new();
        private readonly List<IndirectObjectId> _deletedObjects = new();

        private readonly PdfNavigator _pdfNavigator;

        public IncrementalUpdateManager(PdfNavigator pdfNavigator)
        {
            _pdfNavigator = pdfNavigator ?? throw new ArgumentNullException(nameof(pdfNavigator));
        }

        public async Task<IndirectObject> AddNewObjectAsync(PdfObject pdfObject)
        {
            if (pdfObject is null) throw new ArgumentNullException(nameof(pdfObject));

            IndirectObjectId newObjectId = await GetFreeIndexAsync();

            var indirectObject = new IndirectObject(newObjectId, pdfObject);

            _newObjects.Add(indirectObject);

            return indirectObject;
        }

        public void UpdateObject(IndirectObject indirectObject)
        {
            if (indirectObject is null) throw new ArgumentNullException(nameof(indirectObject));

            _updatedObjects[indirectObject.Id] = indirectObject;
        }

        public void DeleteObject(IndirectObjectId indirectObjectId)
        {
            if (indirectObjectId is null) throw new ArgumentNullException(nameof(indirectObjectId));

            indirectObjectId.GenerationNumber++;

            _deletedObjects.Add(indirectObjectId);
        }

        public async Task SaveAsync(Stream inputStream, Stream outputStream)
        {
            if (inputStream is null) throw new ArgumentNullException(nameof(inputStream));
            if (outputStream is null) throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

            outputStream.Seek(0, SeekOrigin.End);

            foreach (var entry in _newObjects.Concat(_updatedObjects.Values))
            {
                await entry.WriteAsync(outputStream);
            }

            var trailerDictionary = await _pdfNavigator.GetRootTrailerDictionaryAsync();

            var size = trailerDictionary.Size + _newObjects.Count;

            if (_pdfNavigator.UsingXrefStreams)
            {
                IndirectObjectId newObjectId = await GetFreeIndexAsync();

                var xrefStreamIndirectObject = new DummyIndirectObject(newObjectId, outputStream.Position);

                var xrefSections = GenerateCrossReferences(xrefStreamIndirectObject);

                // +1 because the new xref stream should be included in the count
                size++;

                var xrefStreamDict = trailerDictionary as CrossReferenceStreamDictionary
                    ?? throw new InvalidOperationException("Internal Error: {59D30CD9-D2DB-4418-B59E-033538307C68}");

                var prev = trailerDictionary.ByteOffset;

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

                await xrefStreamIndirectObject.WriteAsync(outputStream);
                
                await new Keyword(Constants.StartXref).WriteAsync(outputStream);
                await outputStream.WriteNewLineAsync();

                await new Integer(xrefStreamIndirectObject.ByteOffset!.Value).WriteAsync(outputStream);
                await outputStream.WriteNewLineAsync();

                await new Keyword(Constants.Eof).WriteAsync(outputStream);
            }
            else
            {
                var xrefSections = GenerateCrossReferences(null);

                var xrefTable = new CrossReferenceTable(xrefSections);
                await xrefTable.WriteAsync(outputStream);

                var latestTrailer = (await _pdfNavigator.GetRootTrailerAsync())!;
                var prev = latestTrailer.XrefTableByteOffset;

                var trailer = new Trailer(
                    TrailerDictionary.CreateNew(
                        size,
                        prev,
                        latestTrailer.Dictionary.Root,
                        latestTrailer.Dictionary.Encrypt,
                        latestTrailer.Dictionary.Info,
                        latestTrailer.Dictionary.ID
                        ),
                    xrefTable.ByteOffset!.Value
                    );

                await trailer.WriteAsync(outputStream);
            }

            // TODO: account for the use of features which should increase the pdf version

            // TODO: do we need to amend metadata to change PDF Producer?

            await outputStream.FlushAsync();

            _newObjects.Clear();
            _updatedObjects.Clear();
            _deletedObjects.Clear();

            _pdfNavigator.ClearCache();
        }

        private async Task<IndirectObjectId> GetFreeIndexAsync()
        {
            // Concatenate unsaved entries with existing objects
            var xrefs = (await _pdfNavigator.GetAggregateCrossReferencesAsync())
                .Concat(_newObjects.ToDictionary(e => e.Id.Index, e => new CrossReferenceEntry(0, 0, inUse: true, compressed: false)));

            IndirectObjectId newObjectId;
            var free = xrefs.FirstOrDefault(x => !x.Value.InUse);
            if (free.Key != 0)
            {
                newObjectId = new IndirectObjectId(free.Key, free.Value.Value2);
            }
            else
            {
                newObjectId = new IndirectObjectId(xrefs.Count() + 1, 0);
            }

            return newObjectId;
        }

        private List<CrossReferenceSection> GenerateCrossReferences(IndirectObject? xrefStreamIndirectObject)
        {
            // Every cross reference table starts with the head of the linked list of free entries.
            //CrossReferenceSection? latestXrefSection = new(0);
            CrossReferenceSection? latestXrefSection = null;
            //List<CrossReferenceSection> xrefSections = new() { latestXrefSection };
            List<CrossReferenceSection> xrefSections = new();

            var allEntries =
                //new[] { KeyValuePair.Create(new IndirectObjectId(0, 65535), (IndirectObject?)null) }
                //.Concat(_newObjects.Select(x => KeyValuePair.Create(x.Id, (IndirectObject?)x)))
                _newObjects.Select(x => KeyValuePair.Create(x.Id, (IndirectObject?)x))
                .Concat(_updatedObjects.Select(x => KeyValuePair.Create(x.Key, (IndirectObject?)x.Value)))
                .Concat(_deletedObjects.Select(x => KeyValuePair.Create(x, (IndirectObject?)null)))
                .OrderBy(x => x.Key.Index)
                .ToList();

            if (xrefStreamIndirectObject is not null)
            {
                allEntries.Add(KeyValuePair.Create(xrefStreamIndirectObject.Id, (IndirectObject?)xrefStreamIndirectObject));
            }

            for (var i = allEntries.First().Key.Index; i <= allEntries.Last().Key.Index; i++)
            {
                var entry = allEntries.FirstOrDefault(e => e.Key.Index == i);
                if (entry.Key is not null)
                {
                    if (latestXrefSection is null)
                    {
                        latestXrefSection = new CrossReferenceSection(i);
                        xrefSections.Add(latestXrefSection);
                    }

                    var inUse = entry.Value is not null;
                    var nextFreeObjectNumber = 0; // TODO

                    latestXrefSection.Add(new CrossReferenceEntry(
                        inUse ? entry.Value!.ByteOffset!.Value : nextFreeObjectNumber,
                        entry.Key.GenerationNumber,
                        inUse,
                        false
                        ));
                }
                else
                {
                    // End the section if next entry is non-contiguous
                    latestXrefSection = null;
                }
            }

            return xrefSections;
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
