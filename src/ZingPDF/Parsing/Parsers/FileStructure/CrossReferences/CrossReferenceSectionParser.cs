using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionParser : IParser<CrossReferenceSection>
    {
        private readonly IParser<CrossReferenceSectionIndex> _xrefSectionIndexParser;
        private readonly IParser<CrossReferenceEntry> _xrefEntryParser;

        public CrossReferenceSectionParser(
            IParser<CrossReferenceSectionIndex> xrefSectionIndexParser,
            IParser<CrossReferenceEntry> xrefEntryParser
            )
        {
            _xrefSectionIndexParser = xrefSectionIndexParser;
            _xrefEntryParser = xrefEntryParser;
        }

        public async ITask<CrossReferenceSection> ParseAsync(Stream stream, ObjectContext context)
        {
            // 0 6
            // 0000000003 65535 f
            // 0000000017 00000 n
            // 0000000081 00000 n
            // 0000000000 00007 f
            // 0000000331 00000 n
            // 0000000409 00000 n

            var index = await _xrefSectionIndexParser.ParseAsync(stream, context);
            List<CrossReferenceEntry> entries = new(index.Count);

            // The section header already tells us exactly how many entries follow, so
            // there is no need to re-identify each line before parsing it.
            for (int i = 0; i < index.Count; i++)
            {
                entries.Add(await _xrefEntryParser.ParseAsync(stream, context));
            }

            return new CrossReferenceSection(index.StartIndex, entries, context);
        }
    }
}
