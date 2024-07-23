using MorseCode.ITask;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionParser : IPdfObjectParser<CrossReferenceSection>
    {
        public async ITask<CrossReferenceSection> ParseAsync(Stream stream)
        {
            // 0 6
            // 0000000003 65535 f
            // 0000000017 00000 n
            // 0000000081 00000 n
            // 0000000000 00007 f
            // 0000000331 00000 n
            // 0000000409 00000 n

            var sectionIndexParser = Parser.For<CrossReferenceSectionIndex>();
            var entryParser = Parser.For<CrossReferenceEntry>();

            var index = await sectionIndexParser.ParseAsync(stream);

            Type? type = await TokenTypeIdentifier.TryIdentifyAsync(stream);
            List<CrossReferenceEntry> entries = new();
            long position = stream.Position;

            while (type == typeof(CrossReferenceEntry))
            {
                entries.Add(await entryParser.ParseAsync(stream));

                position = stream.Position;
                type = await TokenTypeIdentifier.TryIdentifyAsync(stream);
            }

            // Since we've read an axtra token we don't need, reset stream
            stream.Position = position;

            return new CrossReferenceSection(index.StartIndex, entries);
        }
    }
}
