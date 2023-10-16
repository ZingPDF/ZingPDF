using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceSectionIndex : PdfObject
    {
        public CrossReferenceSectionIndex(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
        }

        public int StartIndex { get; }
        public int Count { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteIntAsync(StartIndex);
            await stream.WriteWhitespaceAsync();
            await stream.WriteIntAsync(Count);
            await stream.WriteNewLineAsync();
        }
    }
}
