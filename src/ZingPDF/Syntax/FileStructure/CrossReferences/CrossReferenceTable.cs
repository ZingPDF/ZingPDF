using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.FileStructure.CrossReferences
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.4 - Cross-reference table
    /// </summary>
    public class CrossReferenceTable : PdfObject
    {
        public CrossReferenceTable(IEnumerable<CrossReferenceSection> xrefSections, ObjectContext context)
            : base(context)
        {
            Sections = xrefSections ?? throw new ArgumentNullException(nameof(xrefSections));
        }

        public IEnumerable<CrossReferenceSection> Sections { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await new Keyword(Constants.Xref, Context).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            foreach (var section in Sections)
            {
                await section.WriteAsync(stream);
            }
        }

        public override object Clone()
        {
            var clonedSections = Sections.Select(s => (CrossReferenceSection)s.Clone()).ToList();

            return new CrossReferenceTable(clonedSections, Context);
        }
    }
}
