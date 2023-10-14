using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceEntry : PdfObject
    {
        private readonly IndirectObject _indirectObject;

        public CrossReferenceEntry(IndirectObject indirectObject, bool inUse)
        {
            _indirectObject = indirectObject;
            InUse = inUse;
        }

        public ushort GenerationNumber { get; }
        public bool InUse { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Each xref entry is a single line representing an indirect object
            //      0000000017 00000 n
            //               |     | |
            // byte offset __|     | |
            // gen number _________| |
            // free(f) in-use(n)_____|

            // The indirect object may be null, but this should only be for the first entry (the head of the free entries linked list)
            await stream.WriteLongLeftPaddedAsync(_indirectObject?.ByteOffset ?? 0, 10);
            await stream.WriteWhitespaceAsync();

            await stream.WriteIntAsync(GenerationNumber);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(InUse ? "n" : "f");
            await stream.WriteNewLineAsync();
        }
    }
}
