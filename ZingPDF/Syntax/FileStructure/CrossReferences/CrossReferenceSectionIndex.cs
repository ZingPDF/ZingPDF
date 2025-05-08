using ZingPDF.Extensions;

namespace ZingPDF.Syntax.FileStructure.CrossReferences
{
    public class CrossReferenceSectionIndex : PdfObject
    {
        public CrossReferenceSectionIndex(int startIndex, int count, ObjectOrigin objectOrigin)
            : base(objectOrigin)
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

        public override object Clone()
        {
            return new CrossReferenceSectionIndex(StartIndex, Count, Origin);
        }
    }
}
