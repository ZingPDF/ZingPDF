using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// PDF 32000-1:2008 7.5.4
    /// </summary>
    internal class CrossReferenceTable : PdfObject
    {
        private readonly IndirectObjectCollection _indirectObjects;

        public CrossReferenceTable(IndirectObjectCollection indirectObjects)
        {
            _indirectObjects = indirectObjects ?? throw new ArgumentNullException(nameof(indirectObjects));
        }

        public override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteNewLineAsync();

            // Write the cross-reference keyword
            await stream.WriteTextAsync(Constants.Xref);
            await stream.WriteNewLineAsync();

            // Following the keyword, there will be one or more cross-reference sections.
            // Each section contains a contiguous list of object numbers
            // Each section starts with the first object number in its list, followed by the number of entries.
            await stream.WriteIntAsync(0);
            await stream.WriteWhitespaceAsync();
            await stream.WriteIntAsync(_indirectObjects.Count);
            await stream.WriteNewLineAsync();

            foreach(var indirectObject in _indirectObjects)
            {
                //      0000000017 00000 n
                //               |     | |
                // byte offset __|     | |
                // gen number _________| |
                // free(f) in-use(n)_____|

                await stream.WriteLongLeftPaddedAsync(indirectObject.Value?.ByteOffset!.Value ?? 0, 10);
                await stream.WriteWhitespaceAsync();

                await stream.WriteIntAsync(indirectObject.Key.Generation);
                await stream.WriteWhitespaceAsync();

                await stream.WriteTextAsync(indirectObject.Value == null ? "f" : "n");
                await stream.WriteNewLineAsync();
            }
        }
    }
}
