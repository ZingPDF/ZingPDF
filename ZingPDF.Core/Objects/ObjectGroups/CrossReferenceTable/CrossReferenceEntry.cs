using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceEntry : PdfObject
    {
        public CrossReferenceEntry(long indirectObjectByteOffset, ushort indirectObjectGenerationNumber, bool inUse)
        {
            IndirectObjectByteOffset = indirectObjectByteOffset;
            IndirectObjectGenerationNumber = indirectObjectGenerationNumber;
            InUse = inUse;
        }

        public long IndirectObjectByteOffset { get; internal set; }
        public ushort IndirectObjectGenerationNumber { get; }
        public bool InUse { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // Each xref entry is a single line representing an indirect object
            //      0000000017 00000 n
            //               |     | |
            // byte offset __|     | |
            // gen number _________| |
            // free(f) in-use(n)_____|

            await stream.WriteLeftPaddedAsync(IndirectObjectByteOffset, 10);
            await stream.WriteWhitespaceAsync();

            await stream.WriteLeftPaddedAsync(IndirectObjectGenerationNumber, 5);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(InUse ? "n" : "f");
            await stream.WriteNewLineAsync();
        }
    }
}
