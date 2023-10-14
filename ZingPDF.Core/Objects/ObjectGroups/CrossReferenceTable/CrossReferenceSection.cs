namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceSection : PdfObjectGroup
    {
        public CrossReferenceSection(int startIndex, IEnumerable<CrossReferenceEntry> entries)
        {
            if (entries is null) throw new ArgumentNullException(nameof(entries));

            Objects.Add(new CrossReferenceSectionIndex(startIndex, entries.Count()));
            Objects.AddRange(entries);
        }
    }
}
