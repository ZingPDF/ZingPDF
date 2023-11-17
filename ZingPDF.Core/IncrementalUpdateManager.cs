using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable;
using ZingPdf.Core.Objects.ObjectGroups.Trailer;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core
{
    internal class IncrementalUpdateManager
    {
        private readonly Dictionary<IndirectObjectId, IndirectObject> _entries = new();

        public void AddObject(IndirectObject indirectObject)
        {
            if (indirectObject is null)
            {
                throw new ArgumentNullException(nameof(indirectObject));
            }

            _entries[indirectObject.Id] = indirectObject;
        }

        public async Task SaveAsync(Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException("Provided stream must be writable", nameof(stream));
            }

            var pdfTraversal = new StreamPdfTraversal(stream);

            Trailer existingTrailer = await pdfTraversal.GetLatestTrailerAsync();

            stream.Seek(0, SeekOrigin.End);

            foreach (var entry in _entries)
            {
                await entry.Value.WriteAsync(stream);
            }

            var xrefTable = GenerateCrossReferences();
            await xrefTable.WriteAsync(stream);

            existingTrailer.Dictionary.Prev = existingTrailer.XrefTableByteOffset;
            existingTrailer.XrefTableByteOffset = xrefTable.ByteOffset!.Value;
            await existingTrailer.WriteAsync(stream);

            //TODO: account for the use of features which should increase the pdf version

            await stream.FlushAsync();
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
