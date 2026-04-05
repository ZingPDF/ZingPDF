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

        public CrossReferenceTableParser(
            IParser<Keyword> keywordParser,
            IParser<CrossReferenceSection> xrefSectionParser
            )
        {
            _keywordParser = keywordParser;
            _xrefSectionParser = xrefSectionParser;
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
            _ = await _keywordParser.ParseAsync(stream, context);

            List<CrossReferenceSection> sections = [];

            while (PeekNextNonWhitespaceByte(stream) != 't')
            {
                sections.Add(await _xrefSectionParser.ParseAsync(stream, context));
            }

            return new CrossReferenceTable(sections, context);
        }

        private static int PeekNextNonWhitespaceByte(Stream stream)
        {
            long originalPosition = stream.Position;

            while (stream.Position < stream.Length)
            {
                int next = stream.ReadByte();
                if (next < 0)
                {
                    break;
                }

                if (!char.IsWhiteSpace((char)next))
                {
                    stream.Position = originalPosition;
                    return next;
                }
            }

            stream.Position = originalPosition;
            return -1;
        }
    }
}
