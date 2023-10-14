using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    internal class CrossReferenceSectionIndex : PdfObject
    {
        private readonly int _startIndex;
        private readonly int _count;

        public CrossReferenceSectionIndex(int startIndex, int count)
        {
            _startIndex = startIndex;
            _count = count;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteIntAsync(_startIndex);
            await stream.WriteWhitespaceAsync();
            await stream.WriteIntAsync(_count);
            await stream.WriteNewLineAsync();
        }
    }
}
