namespace ZingPdf.Core.Objects
{
    internal abstract class PdfObject
    {
        private long? _byteOffset;

        public bool Written { get; private set; }
        
        public long? ByteOffset
        {
            get
            {
                if (!Written)
                    throw new InvalidOperationException("ByteOffset is not available as the object has not been written to an output stream.");

                return _byteOffset;
            }
            private set => _byteOffset = value;
        }

        public async Task WriteAsync(Stream stream)
        {
            ByteOffset = stream.Position;

            await WriteOutputAsync(stream);

            Written = true;
        }

        public abstract Task WriteOutputAsync(Stream stream);
    }
 }
