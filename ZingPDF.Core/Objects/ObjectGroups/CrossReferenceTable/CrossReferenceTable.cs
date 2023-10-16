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

            foreach(var section in xrefSections)
            {
                for (var i = 0; i < section.Entries.Count(); i++)
                {
                    var entry = section.Entries.ElementAt(i);

                    IndirectObjectLocations.Add(section.Index.StartIndex + i, entry.IndirectObjectByteOffset);
                }
            }
        }

        public Dictionary<int, long> IndirectObjectLocations { get; } = new();
    }
}
