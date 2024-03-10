namespace ZingPdf.Core.Parsing
{
    /// <summary>
    /// <see cref="Stream"/> object which provides access to a source stream within a specified range.
    /// </summary>
    /// <remarks>
    /// This class simply keeps a reference to the source stream, 
    /// but a range is specified which represents the start and end of the stream.<para></para>
    /// The purpose of this class is to provide access to a range within the main stream, 
    /// without duplicating the byte data, and without writing it to memory.<para></para>
    /// This class is not thread safe. Operations on this stream affect the underlying source stream.
    /// Likewise, operations on the source stream will affect instances of this class.
    /// It is good practice to deliberately set the position on this stream before using it. 
    /// </remarks>
    internal class SubStream : Stream
    {
        private readonly Stream _source;
        private readonly long _from;
        private readonly long _to;

        public SubStream(Stream source, long from, long to)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _from = from;
            _to = to;
        }

        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => _source.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _to - _from;

        public override long Position
        {
            get => _source.Position - _from;
            set => _source.Position = value + _from;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_source.Position < _from || _source.Position > _to)
            {
                return 0;
            }

            count = (int)Math.Min(count, _to - Position);

            return _source.Read(buffer, offset, count);
        }

        // TODO: unit tests vital here
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!_source.CanSeek)
            {
                throw new NotSupportedException();
            }

            if (offset > Length)
            {
                throw new InvalidOperationException();
            }

            var adjustedOffset = origin == SeekOrigin.Begin
                ? offset + _from
                : _to - offset;

            _source.Seek(adjustedOffset, origin);

            return Position;
        }

        #region Not Supported
        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        #endregion
    }
}
