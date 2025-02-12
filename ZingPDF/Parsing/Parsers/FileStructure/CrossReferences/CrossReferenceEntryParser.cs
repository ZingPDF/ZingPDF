using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IObjectParser<CrossReferenceEntry>
    {
        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream)
        {
            // 0000000000 65535 f

            var byteOffset = await Parser.Integers.ParseAsync(stream);
            ushort genNumber = await Parser.Integers.ParseAsync(stream);
            string inUse = await Parser.Keywords.ParseAsync(stream);

            return new CrossReferenceEntry(byteOffset, genNumber, inUse == "n", compressed: false);
        }
    }
}
