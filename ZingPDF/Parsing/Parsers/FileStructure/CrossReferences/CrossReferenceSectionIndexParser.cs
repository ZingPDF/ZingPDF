using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndexParser : IParser<CrossReferenceSectionIndex>
    {
        private readonly IParser<Number> _numberParser;

        public CrossReferenceSectionIndexParser(IParser<Number> numberParser)
        {
            _numberParser = numberParser;
        }

        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream, ParseContext context)
        {
            // Example: 0 28
            return new CrossReferenceSectionIndex(
                await _numberParser.ParseAsync(stream, context),
                await _numberParser.ParseAsync(stream, context),
                context.Origin
                );
        }
    }
}
