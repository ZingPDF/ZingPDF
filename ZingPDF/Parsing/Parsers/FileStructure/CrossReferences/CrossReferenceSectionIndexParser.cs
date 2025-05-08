using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndexParser : IObjectParser<CrossReferenceSectionIndex>
    {
        private IPdfContext _pdfContext;

        public CrossReferenceSectionIndexParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream, ParseContext context)
        {
            // Example: 0 28
            return new CrossReferenceSectionIndex(
                await _pdfContext.Parser.Numbers.ParseAsync(stream, context),
                await _pdfContext.Parser.Numbers.ParseAsync(stream, context),
                context.Origin
                );
        }
    }
}
