namespace ZingPDF.Syntax.FileStructure.CrossReferences
{
    public class CrossReferenceSection : PdfObject
    {
        public CrossReferenceSection(int startIndex, IEnumerable<CrossReferenceEntry> entries, ObjectContext context)
            : base(context)
        {
            ArgumentNullException.ThrowIfNull(entries);

            Index = new CrossReferenceSectionIndex(startIndex, entries.Count(), context);
            Entries = [.. entries];
        }

        public CrossReferenceSection(int startIndex, ObjectContext context)
            : this(startIndex, [], context) { }

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

        public override object Clone()
        {
            var clonedEntries = Entries.Select(e => (CrossReferenceEntry)e.Clone()).ToList();

            return new CrossReferenceSection(Index.StartIndex, clonedEntries, Context);
        }
    }
}
