using MorseCode.ITask;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.ObjectGroupParsers.CrossReferenceTableParsing
{
    internal class CrossReferenceSectionIndexParser : IPdfObjectParser<CrossReferenceSectionIndex>
    {
        private readonly IPdfObjectParser<Integer> _integerParser = Parser.For<Integer>();

        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream)
        {
            // Example: 0 28
            return new CrossReferenceSectionIndex(
                await _integerParser.ParseAsync(stream),
                await _integerParser.ParseAsync(stream)
                );
        }
    }
}
