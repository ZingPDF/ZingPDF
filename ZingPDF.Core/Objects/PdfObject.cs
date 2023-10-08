namespace ZingPdf.Core.Objects
{
    public abstract class PdfObject
    {
        private long? _byteOffset;

        public PdfObject(long? length = null)
        {
            // Length may be set when creating an object during parsing,
            // or by this class itself when written to an output stream.
            Length = length;
        }

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

        public long? Length { get; private set; }

        public async Task WriteAsync(Stream stream)
        {
            ByteOffset = stream.Position;

            await WriteOutputAsync(stream);

            Written = true;
            Length = stream.Position - ByteOffset;
        }

        public abstract Task WriteOutputAsync(Stream stream);
    }
 }
