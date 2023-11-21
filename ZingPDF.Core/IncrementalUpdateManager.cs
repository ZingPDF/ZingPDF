using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    // TODO: unit tests are vital for this class
    internal class IncrementalUpdateManager
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject> _entries = new();

        public async Task<IndirectObject> AddNewObjectAsync(PdfObject pdfObject, Stream inputStream)
        {
            if (pdfObject is null) throw new ArgumentNullException(nameof(pdfObject));
            if (inputStream is null) throw new ArgumentNullException(nameof(inputStream));

            var pdfTraversal = new StreamPdfTraversal(inputStream);

            // Concatenate unsaved entries with existing objects
            var xrefs = (await pdfTraversal.GetAggregateCrossReferencesAsync())
                .Concat(_entries.Select(e => new CrossReferenceEntry(0, e.Key.GenerationNumber, inUse: true)));

            var freeIndex = xrefs.Skip(1).ToList().FindIndex(x => !x.InUse);
            IndirectObjectId objectId;
            if (freeIndex == -1)
            {
                freeIndex = xrefs.Count() + 1;
                objectId = new IndirectObjectId(freeIndex, 0);
            }
            else
            {
                var xref = xrefs.ElementAt(freeIndex);
                objectId = new IndirectObjectId(freeIndex, xref.IndirectObjectGenerationNumber);
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

            var pdfTraversal = new StreamPdfTraversal(inputStream);

            var latestTrailer = await pdfTraversal.GetLatestTrailerAsync();

            outputStream.Seek(0, SeekOrigin.End);

            foreach (var entry in _entries)
            {
                await entry.Value.WriteAsync(outputStream);
            }

            var xrefTable = GenerateCrossReferences();
            await xrefTable.WriteAsync(outputStream);

            // TODO: adjust 'Size' value if necessary
            latestTrailer.Dictionary.Prev = latestTrailer.XrefTableByteOffset;
            latestTrailer.XrefTableByteOffset = xrefTable.ByteOffset!.Value;
            await latestTrailer.WriteAsync(outputStream);

            //TODO: account for the use of features which should increase the pdf version

            await outputStream.FlushAsync();
        }

        private CrossReferenceTable GenerateCrossReferences()
        {
            // Every cross reference table starts with the head of the linked list of free entries.
            CrossReferenceSection? latestXrefSection = new(0, new[] { new CrossReferenceEntry(0, 65535, false) });
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
