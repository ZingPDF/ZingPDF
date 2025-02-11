using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndexParser : IObjectParser<CrossReferenceSectionIndex>
    {
        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream)
        {
            // Example: 0 28
            return new CrossReferenceSectionIndex(
                await Parser.Integers.ParseAsync(stream),
                await Parser.Integers.ParseAsync(stream)
                );
        }
    }
}
