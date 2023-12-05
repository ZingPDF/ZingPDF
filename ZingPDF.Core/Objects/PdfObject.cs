namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// Represents a PDF object as described in ISO 32000-2:2020 7.1. This is an abstract class.
    /// </summary>
    public abstract class PdfObject : IPdfObject
    {
        public bool Written { get; private set; }

        public long? ByteOffset { get; internal set; }

        public async Task WriteAsync(Stream stream)
        {
            ByteOffset = stream.Position;

            await WriteOutputAsync(stream);

            Written = true;
        }

        protected abstract Task WriteOutputAsync(Stream stream);
    }
}
