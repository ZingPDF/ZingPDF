using MorseCode.ITask;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IPdfObjectParser<CrossReferenceEntry>
    {
        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream)
        {
            // 0000000000 65535 f

            var integerParser = Parser.For<Integer>();

            var byteOffset = await integerParser.ParseAsync(stream);
            ushort genNumber = await integerParser.ParseAsync(stream);
            string inUse = await Parser.For<Keyword>().ParseAsync(stream);

            return new CrossReferenceEntry(byteOffset, genNumber, inUse == "n", compressed: false);
        }
    }
}
