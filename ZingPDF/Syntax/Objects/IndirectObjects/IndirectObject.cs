using ZingPDF.Extensions;

namespace ZingPDF.Syntax.Objects.IndirectObjects
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.10 - Indirect objects<para></para>
    /// 
    /// Wraps any object with an identifier so that it may be referenced by other objects.
    /// </summary>
    public class IndirectObject : PdfObject
    {
        public IndirectObject(IndirectObjectId id, params IPdfObject[] children)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Children = children?.ToList() ?? throw new ArgumentNullException(nameof(children));
        }

        public IndirectObjectId Id { get; }
        public List<IPdfObject> Children { get; }

        public T Get<T>(int index = 0) => (T)Children[index];

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // e.g.
            // 8 0 obj
            // 77
            // endobj

            // Object number
            await stream.WriteIntAsync(Id.Index);
            await stream.WriteWhitespaceAsync();

            // Generation number
            await stream.WriteIntAsync(Id.GenerationNumber);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Constants.ObjStart);
            await stream.WriteNewLineAsync();

            foreach (PdfObject child in Children)
            {
                await child.WriteAsync(stream);
            }

            await stream.WriteNewLineAsync();
            await stream.WriteTextAsync(Constants.ObjEnd);
            await stream.WriteNewLineAsync();
        }
    }
}
