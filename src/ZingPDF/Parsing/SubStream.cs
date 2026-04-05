namespace ZingPDF.Parsing
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
    /// </remarks>
    internal class SubStream : Stream
    {
        private readonly Stream _source;

        public SubStream(Stream source, long from, long to, bool setToStart = true)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            From = from;
            To = to;

            if (setToStart)
            {
                Position = 0;
            }
        }

        public long From { get; }
        public long To { get; }

        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => _source.CanSeek;
        public override bool CanWrite => false;
        public override long Length => To - From;

        public override long Position
        {
            get => _source.Position - From;
            set => _source.Position = value + From;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_source.Position < From || _source.Position > To)
            {
                return 0;
            }

            count = (int)Math.Min(count, To - From - Position);

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
                ? offset + From
                : To - offset;

            _source.Seek(adjustedOffset, origin);

            return Position;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region Not Supported
        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        #endregion
    }
}
