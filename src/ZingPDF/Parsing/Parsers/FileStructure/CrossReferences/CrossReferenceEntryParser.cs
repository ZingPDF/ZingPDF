using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax;
using ZingPDF.Syntax.FileStructure.CrossReferences;

namespace ZingPDF.Parsing.Parsers.FileStructure.CrossReferences
{
    internal class CrossReferenceEntryParser : IParser<CrossReferenceEntry>
    {
        public CrossReferenceEntryParser() { }

        public async ITask<CrossReferenceEntry> ParseAsync(Stream stream, ObjectContext context)
        {
            // 0000000000 65535 f
            stream.AdvancePastWhitepace();

            int byteOffset = ReadIntToken(stream);
            ushort genNumber = checked((ushort)ReadIntToken(stream));
            byte inUse = ReadSingleByteToken(stream);

            await Task.CompletedTask;
            return new CrossReferenceEntry(byteOffset, genNumber, inUse == 'n', compressed: false, context);
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

        private static byte ReadSingleByteToken(Stream stream)
        {
            stream.AdvancePastWhitepace();

            int next = stream.ReadByte();
            if (next < 0)
            {
                throw new ParserException("Unexpected end of xref entry.");
            }

            return (byte)next;
        }
    }
}
