namespace ZingPdf.Core.Objects.ObjectGroups
{
    internal class PdfObjectGroup : PdfObject
    {
        public List<PdfObject> Objects { get; private set; } = new();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in Objects)
            {
                await obj.WriteAsync(stream);
            }
        }

        public T Get<T>(int index) where T : PdfObject
            => (T)Objects[index];

        public static implicit operator PdfObjectGroup(List<PdfObject> items) => new() { Objects = items };
        public static implicit operator PdfObjectGroup(PdfObject[] items) => new() { Objects = items.ToList() };
    }
}
