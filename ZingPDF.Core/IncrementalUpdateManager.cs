using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    // TODO: unit tests are vital for this class
    internal class IncrementalUpdateManager
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject> _entries = new();
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
                .Concat(_entries.ToDictionary(e => e.Key.Index, e => new CrossReferenceEntry(0, e.Key.GenerationNumber, inUse: true, compressed: false)));

            var freeIndex = xrefs.Skip(1).ToList().FindIndex(x => !x.Value.InUse);
            IndirectObjectId objectId;
            if (freeIndex == -1)
            {
                freeIndex = xrefs.Count() + 1;
                objectId = new IndirectObjectId(freeIndex, 0);
            }
            else
            {
                var xref = xrefs.ElementAt(freeIndex);
                objectId = new IndirectObjectId(freeIndex, xref.Value.Value2);
            }

            var indirectObject = new IndirectObject(objectId, pdfObject);

            _entries[indirectObject.Id] = indirectObject;

            return indirectObject;
        }

        public void UpdateObject(IndirectObject indirectObject)
        {
            if (indirectObject is null) throw new ArgumentNullException(nameof(indirectObject));

            _entries[indirectObject.Id] = indirectObject;
        }

        public async Task SaveAsync(Stream inputStream, Stream outputStream)
        {
            if (inputStream is null) throw new ArgumentNullException(nameof(inputStream));
            if (outputStream is null) throw new ArgumentNullException(nameof(outputStream));
            if (!outputStream.CanWrite) throw new ArgumentException("Provided output stream must be writable", nameof(outputStream));

            if (!_entries.Any())
            {
                return;
            }

            outputStream.Seek(0, SeekOrigin.End);

            foreach (var entry in _entries)
            {
                await entry.Value.WriteAsync(outputStream);
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

            // There shall be one xref section for each contiguous block of entries
            var keys = _entries.Keys.OrderBy(k => k.Index);

            for (var i = 1; i <= keys.Last().Index; i++)
            {
                var key = keys.FirstOrDefault(k => k.Index == i);
                if (key is not null)
                {
                    if (latestXrefSection is null)
                    {
                        latestXrefSection = new CrossReferenceSection(i);
                        xrefSections.Add(latestXrefSection);
                    }

                    var entry = _entries[key];

                    latestXrefSection.Add(entry.ByteOffset, entry.Id.GenerationNumber);
                }
                else
                {
                    latestXrefSection = null;
                }
            }

            var xrefTable = new CrossReferenceTable(xrefSections);

            _entries.Clear();

            return xrefTable;
        }
    }
}
