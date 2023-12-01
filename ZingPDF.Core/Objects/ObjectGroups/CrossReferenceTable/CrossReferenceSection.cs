namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
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

        public void Add(long? byteOffset, ushort generationNumber)
        {
            // TODO: this seems awfully disconnected from the indirect object itself
            // How do we ensure the index we're adding is the same as the indirect object we're referencing.
            Index.Count++;
            Entries.Add(new CrossReferenceEntry(byteOffset ?? 0, generationNumber, inUse: true, compressed: false));
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
