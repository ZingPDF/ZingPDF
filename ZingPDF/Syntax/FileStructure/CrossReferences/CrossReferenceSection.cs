namespace ZingPDF.Syntax.FileStructure.CrossReferences
{
    internal class CrossReferenceSection : PdfObject
    {
        public CrossReferenceSection(int startIndex)
            : this(startIndex, new List<CrossReferenceEntry>())
        { }

        public CrossReferenceSection(int startIndex, IEnumerable<CrossReferenceEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            Index = new CrossReferenceSectionIndex(startIndex, entries.Count());
            Entries = entries.ToList();
        }

        public CrossReferenceSectionIndex Index { get; }
        public List<CrossReferenceEntry> Entries { get; }

        public void Add(CrossReferenceEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            Index.Count++;
            Entries.Add(entry);
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await Index.WriteAsync(stream);

            foreach (CrossReferenceEntry entry in Entries)
            {
                await entry.WriteAsync(stream);
            }
        }
    }
}
