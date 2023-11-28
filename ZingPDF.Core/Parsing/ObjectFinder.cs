using System.Text;

namespace ZingPdf.Core.Parsing
{
    internal class ObjectFinder
    {
        private readonly int _bufferSize = 1024;

        public async Task<long?> FindAsync(Stream stream, string token, bool forwards = true, int limit = 1024)
        {
            if (!stream.CanSeek)
            {
                throw new ParserException();
            }

            stream.Seek(0, forwards ? SeekOrigin.Begin : SeekOrigin.End);

            byte[] buffer = new byte[_bufferSize];

            var found = false;
            string content = string.Empty;

            do
            {
                // Calculate the amount left to read.
                // When going backwards (for non-linearized PDFs), this is the smaller of the buffer size and remaining data.
                // When going forwards this can simply be the buffer size;
                int readSize = forwards ? _bufferSize : (int)Math.Min(limit, Math.Min(_bufferSize, stream.Position));

                // When reading a stream, we always go forwards.
                // Therefore when going backwards, seek back by the read size.
                // The stream position will be reset after reading.
                if (!forwards)
                {
                    stream.Seek(-readSize, SeekOrigin.Current);
                }

                var read = await stream.ReadAsync(buffer.AsMemory(0, readSize));

                if (!forwards)
                {
                    stream.Seek(-readSize, SeekOrigin.Current);
                }

                var readContent = Encoding.ASCII.GetString(buffer, 0, readSize);
                content = forwards ? content + readContent : readContent + content;

                var index = content.IndexOf(token);
                if (index != -1)
                {
                    found = true;
                    
                    if (forwards)
                    {
                        stream.Position = index;
                    }
                    else
                    {
                        stream.Position += index;
                    }

                    break;
                }
            }
            while (forwards
                ? stream.Position <= limit
                : stream.Position >= stream.Length - limit);

            return found ? stream.Position : null;
        }
    }
}
