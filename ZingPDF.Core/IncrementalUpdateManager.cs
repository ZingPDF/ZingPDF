using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
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

        public async Task<IndirectObject> AddNewObjectAsync(PdfObject pdfObject, Stream inputStream)
        {
            if (pdfObject is null) throw new ArgumentNullException(nameof(pdfObject));
            if (inputStream is null) throw new ArgumentNullException(nameof(inputStream));

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

            _deletedObjects.Add(indirectObjectId);
        }

        public async Task SaveAsync(Stream inputStream, Stream outputStream)
        {
            if (inputStream is null) throw new ArgumentNullException(nameof(inputStream));
            if (outputStream is null) throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

            // new - write object, write xref
            // updated - write object, write xref
            // deleted - only write free xref entry

            outputStream.Seek(0, SeekOrigin.End);

            foreach (var entry in _newObjects.Concat(_updatedObjects.Values))
            {
                await entry.WriteAsync(outputStream);
            }

            var xrefTable = GenerateCrossReferences();
            await xrefTable.WriteAsync(outputStream);

            var latestTrailer = await _pdfNavigator.GetRootTrailerAsync();

            if (latestTrailer is null)
            {
                var trailerDictionary = await _pdfNavigator.GetRootTrailerDictionaryAsync();

                // TODO: adjust 'Size' value if necessary
                var newTrailerDictionary = TrailerDictionary.CreateNew(
                    size: trailerDictionary.Size,
                    prev: trailerDictionary.ByteOffset,
                    root: trailerDictionary.Root,
                    encrypt: trailerDictionary.Encrypt,
                    info: trailerDictionary.Info,
                    id: trailerDictionary.ID
                );

                latestTrailer = new Trailer(newTrailerDictionary, xrefTable.ByteOffset!.Value);
            }
            else
            {
                // TODO: adjust 'Size' value if necessary
                latestTrailer.Dictionary.Prev = latestTrailer.XrefTableByteOffset;
                latestTrailer.XrefTableByteOffset = xrefTable.ByteOffset!.Value;
            }

            await latestTrailer.WriteAsync(outputStream);

            // TODO: account for the use of features which should increase the pdf version

            // TODO: do we need to amend metadata to change PDF Producer?

            await outputStream.FlushAsync();

            _pdfNavigator.ClearCache();
        }

        private CrossReferenceTable GenerateCrossReferences()
        {
            // Every cross reference table starts with the head of the linked list of free entries.
            CrossReferenceSection? latestXrefSection = new(0, new[] { new CrossReferenceEntry(0, 65535, false, compressed: false) });
            List<CrossReferenceSection> xrefSections = new() { latestXrefSection };

            var allEntries = 
                _newObjects
                .Concat(_updatedObjects.Values)
                .Concat(_updatedObjects.Values)
                .OrderBy(x => x.Id.Index);

            // There shall be one xref section for each contiguous block of entries
            //var keys = _entries.Keys.OrderBy(k => k.Index);

            for (var i = allEntries.First().Id.Index; i <= allEntries.Last().Id.Index; i++)
            {
                var entry = allEntries.FirstOrDefault(e => e.Id.Index == i);
                if (entry is not null)
                {
                    if (latestXrefSection is null)
                    {
                        latestXrefSection = new CrossReferenceSection(i);
                        xrefSections.Add(latestXrefSection);
                    }

                    latestXrefSection.Add(entry.ByteOffset, entry.Id.GenerationNumber);
                }
                else
                {
                    // End the section if next entry is non-contiguous
                    latestXrefSection = null;
                }
            }

            var xrefTable = new CrossReferenceTable(xrefSections);

            _newObjects.Clear();
            _updatedObjects.Clear();
            _deletedObjects.Clear();

            return xrefTable;
        }
    }
}
