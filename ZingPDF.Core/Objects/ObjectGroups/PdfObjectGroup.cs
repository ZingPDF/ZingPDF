namespace ZingPdf.Core.Objects.ObjectGroups
{
    internal abstract class PdfObjectGroup : PdfObject
    {
        protected List<PdfObject> Objects { get; } = new();

        public override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in Objects)
            {
                await obj.WriteOutputAsync(stream);
            }
        }
    }
}
