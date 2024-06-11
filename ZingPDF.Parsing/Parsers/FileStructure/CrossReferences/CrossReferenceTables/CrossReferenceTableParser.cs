using MorseCode.ITask;
using ZingPDF.ObjectModel.FileStructure.CrossReferences;
using ZingPDF.ObjectModel.FileStructure.Trailer;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences.CrossReferenceTables
{
    internal class CrossReferenceTableParser : IPdfObjectParser<CrossReferenceTable>
    {
        public async ITask<CrossReferenceTable> ParseAsync(Stream stream)
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

            var sectionParser = Parser.For<CrossReferenceSection>();

            List<CrossReferenceSection> sections = new();

            Type? currentType = await TokenTypeIdentifier.TryIdentifyAsync(stream);

            while (currentType != null && currentType != typeof(CrossReferenceEntry) && currentType != typeof(Keyword) && currentType != typeof(Trailer))
            {
                sections.Add(await sectionParser.ParseAsync(stream));

                currentType = await TokenTypeIdentifier.TryIdentifyAsync(stream);
            }

            return new CrossReferenceTable(sections);
        }
    }
}
