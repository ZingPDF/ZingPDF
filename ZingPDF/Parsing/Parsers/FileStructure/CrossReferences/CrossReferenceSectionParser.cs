using MorseCode.ITask;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionParser : IParser<CrossReferenceSection>
    {
        private readonly IParser<CrossReferenceSectionIndex> _xrefSectionIndexParser;
        private readonly IParser<CrossReferenceEntry> _xrefEntryParser;
        private readonly ITokenTypeIdentifier _tokenTypeIdentifier;

        public CrossReferenceSectionParser(
            IParser<CrossReferenceSectionIndex> xrefSectionIndexParser,
            IParser<CrossReferenceEntry> xrefEntryParser,
            ITokenTypeIdentifier tokenTypeIdentifier
            )
        {
            _xrefSectionIndexParser = xrefSectionIndexParser;
            _xrefEntryParser = xrefEntryParser;
            _tokenTypeIdentifier = tokenTypeIdentifier;
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

            Type? type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);
            List<CrossReferenceEntry> entries = [];
            long position = stream.Position;

            while (type == typeof(CrossReferenceEntry))
            {
                entries.Add(await _xrefEntryParser.ParseAsync(stream, context));

                position = stream.Position;
                type = await _tokenTypeIdentifier.TryIdentifyAsync(stream);
            }

            // Since we've read an axtra token we don't need, reset stream
            stream.Position = position;

            return new CrossReferenceSection(index.StartIndex, entries, context);
        }
    }
}
