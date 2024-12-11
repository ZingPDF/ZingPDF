using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IPdfObjectParser<CrossReferenceEntry>
    {
        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            // 0000000000 65535 f

            var byteOffset = await Parser.Integers.ParseAsync(stream, indirectObjectDictionary);
            ushort genNumber = await Parser.Integers.ParseAsync(stream, indirectObjectDictionary);
            string inUse = await Parser.Keywords.ParseAsync(stream, indirectObjectDictionary);

            return new CrossReferenceEntry(byteOffset, genNumber, inUse == "n", compressed: false);
        }
    }
}
