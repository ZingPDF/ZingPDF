using MorseCode.ITask;
using ZingPdf.Core.Objects.ObjectGroups.CrossReferences;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectGroupParsers.CrossReferenceTableParsing
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
