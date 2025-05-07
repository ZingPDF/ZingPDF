using ZingPDF.Syntax;

namespace ZingPDF.Parsing
{
    internal class PdfObjectGroup(IEnumerable<IPdfObject> objects, ObjectOrigin origin)
        : PdfObject(origin)
    {
        public List<IPdfObject> Objects { get; } = [.. objects];

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in Objects)
            {
                await obj.WriteAsync(stream);
            }
        }

        public T Get<T>(int index) where T : IPdfObject
            => (T)Objects[index];

        public override object Clone()
        {
            var clone = new PdfObjectGroup(Objects.Select(x => (IPdfObject)x.Clone()), Origin);

            return clone;
        }
    }
}
