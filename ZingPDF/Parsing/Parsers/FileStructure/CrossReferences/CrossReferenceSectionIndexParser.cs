using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndexParser : IPdfObjectParser<CrossReferenceSectionIndex>
    {
        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            // Example: 0 28
            return new CrossReferenceSectionIndex(
                await Parser.Integers.ParseAsync(stream, indirectObjectDictionary),
                await Parser.Integers.ParseAsync(stream, indirectObjectDictionary)
                );
        }
    }
}
