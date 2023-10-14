namespace ZingPdf.Core.Objects.ObjectGroups
{
    internal abstract class PdfObjectGroup : PdfObject
    {
        protected List<PdfObject> Objects { get; } = new();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in Objects)
            {
                await obj.WriteAsync(stream);
            }
        }
    }
}
