using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IParser<CrossReferenceEntry>
    {
        private readonly IParser<Number> _numberParser;
        private readonly IParser<Keyword> _keywordParser;
        
        public CrossReferenceEntryParser(IParser<Number> numberParser, IParser<Keyword> keywordParser)
        {   
            _numberParser = numberParser;
            _keywordParser = keywordParser;
        }

        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream, ParseContext context)
        {
            // 0000000000 65535 f

            var byteOffset = await _numberParser.ParseAsync(stream, context);
            ushort genNumber = await _numberParser.ParseAsync(stream, context);
            string inUse = await _keywordParser.ParseAsync(stream, context);

            return new CrossReferenceEntry(byteOffset, genNumber, inUse == "n", compressed: false, context.Origin);
        }
    }
}
