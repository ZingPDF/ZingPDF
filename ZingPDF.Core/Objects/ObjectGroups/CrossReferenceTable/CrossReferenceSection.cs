namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceSection : PdfObjectGroup
    {
        public CrossReferenceSection(int startIndex, IEnumerable<CrossReferenceEntry> entries)
        {
            if (entries is null) throw new ArgumentNullException(nameof(entries));

            Index = new CrossReferenceSectionIndex(startIndex, entries.Count());
            Entries = entries;

            Objects.Add(Index);
            Objects.AddRange(entries);
        }

        public CrossReferenceSectionIndex Index { get; }
        public IEnumerable<CrossReferenceEntry> Entries { get; }
    }
}
