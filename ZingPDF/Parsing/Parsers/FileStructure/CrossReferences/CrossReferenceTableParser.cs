using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceTableParser : IParser<CrossReferenceTable>
    {
        private readonly IParser<Keyword> _keywordParser;
        private readonly IParser<CrossReferenceSection> _xrefSectionParser;
        private readonly ITokenTypeIdentifier _tokenTypeIdentifier;

        public CrossReferenceTableParser(
            IParser<Keyword> keywordParser,
            IParser<CrossReferenceSection> xrefSectionParser,
            ITokenTypeIdentifier tokenTypeIdentifier
            )
        {
            _keywordParser = keywordParser;
            _xrefSectionParser = xrefSectionParser;
            _tokenTypeIdentifier = tokenTypeIdentifier;
        }

        public async ITask<CrossReferenceTable> ParseAsync(Stream stream, ObjectContext context)
        {
            // Example: xref sections
            // 
            // xref
            // 0 1
            // 0000000000 65535 f
            // 3 1
            // 0000025325 00000 n
            // 23 2
            // 0000025518 00002 n
            // 0000025635 00000 n
            // 30 1
            // 0000025777 00000 n

            // Ignore the xref keyword
            _ = _keywordParser.ParseAsync(stream, context);

            List<CrossReferenceSection> sections = [];

            Type? currentType = await _tokenTypeIdentifier.TryIdentifyAsync(stream);

            while (
                currentType != null
                && currentType != typeof(CrossReferenceEntry)
                && currentType != typeof(Keyword)
                && currentType != typeof(Trailer)
                )
            {
                sections.Add(await _xrefSectionParser.ParseAsync(stream, context));

                currentType = await _tokenTypeIdentifier.TryIdentifyAsync(stream);
            }

            return new CrossReferenceTable(sections, context);
        }
    }
}
