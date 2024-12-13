using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceTableParser : IPdfObjectParser<CrossReferenceTable>
    {
        public async ITask<CrossReferenceTable> ParseAsync(Stream stream, IIndirectObjectDictionary indirectObjectDictionary)
        {
            // Example: xref sections
            // 
            // xref
            // 0 1
            // 0000000000 65535 f
            // 3 1
            // 0000025325 00000 n
            // 23 2
            // 0000025518 00002 n
            // 0000025635 00000 n
            // 30 1
            // 0000025777 00000 n

            // Ignore the xref keyword
            _ = Parser.Keywords.ParseAsync(stream, indirectObjectDictionary);

            List<CrossReferenceSection> sections = [];

            Type? currentType = await TokenTypeIdentifier.TryIdentifyAsync(stream);

            while (currentType != null && currentType != typeof(CrossReferenceEntry) && currentType != typeof(Keyword) && currentType != typeof(Trailer))
            {
                sections.Add(await Parser.XrefSections.ParseAsync(stream, indirectObjectDictionary));

                currentType = await TokenTypeIdentifier.TryIdentifyAsync(stream);
            }

            return new CrossReferenceTable(sections);
        }
    }
}
