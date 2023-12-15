namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferences
{
    internal class CrossReferenceSection : PdfObject
    {
        public CrossReferenceSection(int startIndex)
            : this(startIndex, new List<CrossReferenceEntry>())
        { }

        public CrossReferenceSection(int startIndex, IEnumerable<CrossReferenceEntry> entries)
        {
            if (entries is null) throw new ArgumentNullException(nameof(entries));

            Index = new CrossReferenceSectionIndex(startIndex, entries.Count());
            Entries = entries.ToList();
        }

        public CrossReferenceSectionIndex Index { get; }
        public List<CrossReferenceEntry> Entries { get; }

        public void Add(CrossReferenceEntry entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));

            Index.Count++;
            Entries.Add(entry);
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await Index.WriteAsync(stream);
            
            foreach(CrossReferenceEntry entry in Entries)
            {
                await entry.WriteAsync(stream);
            }
        }
    }
}
