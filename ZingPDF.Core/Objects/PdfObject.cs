namespace ZingPdf.Core.Objects
{
    public abstract class PdfObject
    {
        public bool Written { get; private set; }
        
        public long? ByteOffset { get; private set; }

        public async Task WriteAsync(Stream stream)
        {
            ByteOffset = stream.Position;

            await WriteOutputAsync(stream);

            Written = true;
        }

        protected abstract Task WriteOutputAsync(Stream stream);
    }
 }
