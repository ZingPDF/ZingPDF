using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndexParser : IParser<CrossReferenceSectionIndex>
    {
        public CrossReferenceSectionIndexParser() { }

        public async ITask<CrossReferenceSectionIndex> ParseAsync(Stream stream, ObjectContext context)
        {
            // Example: 0 28
            stream.AdvancePastWhitepace();

            await Task.CompletedTask;
            return new CrossReferenceSectionIndex(
                ReadIntToken(stream),
                ReadIntToken(stream),
                context
                );
        }

        private static int ReadIntToken(Stream stream)
        {
            stream.AdvancePastWhitepace();

            int value = 0;

            while (stream.Position < stream.Length)
            {
                int next = stream.ReadByte();
                if (next < 0)
                {
                    break;
                }

                if (char.IsWhiteSpace((char)next))
                {
                    break;
                }

                value = (value * 10) + (next - '0');
            }

            return value;
        }
    }
}
