using System.Text;

namespace ZingPdf.Core.Parsing
{
    internal class TrailerFinder
    {
        private readonly int _bufferSize = 1024;

        public async Task<long?> FindAsync(Stream stream, bool linearizedPdf)
        {
            if (!stream.CanSeek)
            {
                throw new ParserException();
            }

            stream.Seek(0, linearizedPdf ? SeekOrigin.Begin : SeekOrigin.End);

            byte[] buffer = new byte[_bufferSize];

            string content = string.Empty;

            do
            {
                // Calculate the amount left to read.
                // When going backwards (for non-linearized PDFs), this is the smaller of the buffer size and remaining data.
                // When going forwards this can simply be the buffer size;
                int readSize = linearizedPdf ? _bufferSize : (int)Math.Min(_bufferSize, stream.Position);

                // When reading a stream, we always go forwards.
                // Therefore when going backwards, seek back by the read size.
                // The stream position will be reset after reading.
                if (!linearizedPdf)
                {
                    stream.Seek(-readSize, SeekOrigin.Current);
                }

                await stream.ReadAsync(buffer.AsMemory(0, readSize));

                if (!linearizedPdf)
                {
                    stream.Seek(-readSize, SeekOrigin.Current);
                }

                var readContent = Encoding.UTF8.GetString(buffer, 0, readSize);
                content = linearizedPdf ? content + readContent : readContent + content;

                var index = content.IndexOf(Constants.Trailer);
                if (index != -1)
                {
                    stream.Position += index;
                    break;
                }
            }
            while (stream.Position < stream.Length);

            return stream.Position > 0 ? stream.Position : null;
        }
    }
}
