using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IObjectParser<CrossReferenceEntry>
    {
        private IPdfContext _pdfContext;

        public CrossReferenceEntryParser(IPdfContext pdfContext)
        {
            _pdfContext = pdfContext;
        }

        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream, ParseContext context)
        {
            // 0000000000 65535 f

            var byteOffset = await _pdfContext.Parser.Numbers.ParseAsync(stream, context);
            ushort genNumber = await _pdfContext.Parser.Numbers.ParseAsync(stream, context);
            string inUse = await _pdfContext.Parser.Keywords.ParseAsync(stream, context);

            return new CrossReferenceEntry(byteOffset, genNumber, inUse == "n", compressed: false, context.Origin);
        }
    }
}
