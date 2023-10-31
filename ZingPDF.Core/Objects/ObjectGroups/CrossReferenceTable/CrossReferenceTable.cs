using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.4 - Cross-reference table
    /// </summary>
    internal class CrossReferenceTable : PdfObjectGroup
    {
        public CrossReferenceTable(IEnumerable<CrossReferenceSection> xrefSections)
        {
            if (xrefSections is null) throw new ArgumentNullException(nameof(xrefSections));

            Objects.Add(new Keyword(Constants.Xref));
            Objects.AddRange(xrefSections);

            ExtractIndirectObjectLocations(xrefSections);
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
        public void UpdateByteOffsets()
        {

        }

        private void ExtractIndirectObjectLocations(IEnumerable<CrossReferenceSection> xrefSections)
        {
            // TODO: incremental updates may cause multiple objects with the same object identifier (object number and generation number)
            // This class needs to provide access to all objects (which the IndirectObjectLocations property does not support)
            // This class also needs to provide access to the latest object for a given identifier.

            foreach (var section in xrefSections)
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
