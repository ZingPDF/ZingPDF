using ZingPDF.Extensions;
using ZingPDF.ObjectModel;

namespace ZingPDF.Parsing
{
    internal class PdfObjectGroup : PdfObject
    {
        public List<IPdfObject> Objects { get; private set; } = new();

        protected override async Task WriteOutputAsync(Stream stream)
        {
            foreach (var obj in Objects)
            {
                await obj.WriteAsync(stream);
            }
        }

        public T Get<T>(int index) where T : IPdfObject
            => (T)Objects[index];

        public static implicit operator PdfObjectGroup(List<IPdfObject> items) => new() { Objects = items };
        public static implicit operator PdfObjectGroup(IPdfObject[] items) => new() { Objects = [..items] };

        protected void InsertNewLine() => Objects.Add(new NewLineObject());

        /// <summary>
        /// Convenience class for separating objects within a <see cref="PdfObject"/> with a new line.
        /// </summary>
        private class NewLineObject : PdfObject
        {
            protected override async Task WriteOutputAsync(Stream stream)
            {
                await stream.WriteNewLineAsync();
            }
        }
    }
}
