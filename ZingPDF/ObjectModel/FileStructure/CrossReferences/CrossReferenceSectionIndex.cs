using ZingPDF.Extensions;

namespace ZingPDF.ObjectModel.FileStructure.CrossReferences
{
    internal class CrossReferenceSectionIndex : PdfObject
    {
        public CrossReferenceSectionIndex(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
        }

        public int StartIndex { get; }
        public int Count { get; internal set; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteIntAsync(StartIndex);
            await stream.WriteWhitespaceAsync();
            await stream.WriteIntAsync(Count);
            await stream.WriteNewLineAsync();
        }
    }
}
