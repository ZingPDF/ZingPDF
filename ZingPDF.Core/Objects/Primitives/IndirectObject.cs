using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.10 - Indirect objects
    /// 
    /// Wraps any object with an identifier so that it may be referenced by other objects.
    /// </summary>
    internal class IndirectObject : PdfObject
    {
        private readonly IEnumerable<PdfObject> _children;

        public IndirectObject(IndirectObjectReference id, IEnumerable<PdfObject> children)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public IndirectObjectReference Id { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            // e.g.
            // 8 0 obj
            // 77
            // endobj

            // Object number
            await stream.WriteIntAsync(Id.Id);
            await stream.WriteWhitespaceAsync();

            // Generation number
            await stream.WriteIntAsync(Id.Generation);
            await stream.WriteWhitespaceAsync();

            await stream.WriteTextAsync(Constants.ObjStart);
            await stream.WriteNewLineAsync();

            foreach (PdfObject child in _children)
            {
                await child.WriteAsync(stream);
            }

            await stream.WriteNewLineAsync();
            await stream.WriteTextAsync(Constants.ObjEnd);
            await stream.WriteNewLineAsync();
        }
    }
}
