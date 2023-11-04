using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.4 - Cross-reference table
    /// </summary>
    internal class CrossReferenceTable : PdfObject
    {
        private readonly IEnumerable<CrossReferenceSection> _xrefSections;

        public CrossReferenceTable(IEnumerable<CrossReferenceSection> xrefSections)
        {
            _xrefSections = xrefSections ?? throw new ArgumentNullException(nameof(xrefSections));

            ExtractIndirectObjectLocations();
        }

        /// <summary>
        /// A list of all indirect object indices and their byte offsets.
        /// This is a list of pairs rather than a dictionary as there can be duplicate keys.
        /// </summary>
        public List<KeyValuePair<int, long>> IndirectObjectLocations { get; } = new();

        /// <summary>
        /// Given a parsed PDF, it is important to keep the xref table intact, as it contains file updates.
        /// The specified byte offsets for each object will likely be incorrect when the file is re-written,
        /// due to whitespace/line-break differences. This method is used to update the byte offsets of 
        /// all existing records, once the objects have been written, and we know their new offsets.
        /// </summary>
        public void UpdateByteOffsets(IEnumerable<IndirectObject> objects)
        {
            foreach(IndirectObject indirectObject in objects)
            {
                foreach (var section in _xrefSections)
                {
                    for (var i = section.Index.StartIndex; i < section.Entries.Count(); i++)
                    {
                        if (indirectObject.Id.Index == i)
                        {
                            section.Entries.ElementAt(i).IndirectObjectByteOffset = indirectObject.ByteOffset!.Value;
                            break;
                        }
                    }
                }

                IndirectObjectLocations.Clear();

                ExtractIndirectObjectLocations();
            }
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await new Keyword(Constants.Xref).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            foreach(var section in _xrefSections)
            {
                await section.WriteAsync(stream);
            }
        }

        private void ExtractIndirectObjectLocations()
        {
            foreach (var section in _xrefSections)
            {
                for (var i = 0; i < section.Entries.Count(); i++)
                {
                    var entry = section.Entries.ElementAt(i);

                    IndirectObjectLocations.Add(new KeyValuePair<int, long>(section.Index.StartIndex + i, entry.IndirectObjectByteOffset));
                }
            }
        }
    }
}
