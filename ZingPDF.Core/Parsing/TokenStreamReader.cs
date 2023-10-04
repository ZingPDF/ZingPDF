using System.Diagnostics.CodeAnalysis;
using System.Text;
using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Parsing
{
    /// <summary>
    /// Provides tokenised access to the contents of a stream.
    /// </summary>
    /// <remarks>
    /// Given a byte stream representing a PDF document, this class allows random, forward, and backward access to the tokens within.
    /// 
    /// In practice, this is used by the <see cref="PdfParser"/> to find and read the document trailer from the end of the file, before accessing other PDF objects by their byte offsets.
    /// 
    /// The provided stream must be seekable.
    /// </remarks>
    internal class TokenStreamReader : IDisposable
    {
        private readonly int _bufferSize = 1024;
        private readonly Stream _stream;

        private readonly Dictionary<long, Token> _tokens = new();

        private long _currentByteOffset;

        public TokenStreamReader(Stream stream, ReadDirection readDirection = ReadDirection.Forward)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));     
            if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable", nameof(stream));

            ReadDirection = readDirection;

            if (readDirection == ReadDirection.Forward)
            {
                _stream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                _stream.Seek(0, SeekOrigin.End);
            }

            _currentByteOffset = _stream.Position;
        }

        public ReadDirection ReadDirection { get; set; }

        public void Seek(int offset, SeekOrigin seekOrigin)
        {
            _stream.Seek(offset, seekOrigin);
        }

        /// <summary>
        /// Get the next token in the stream
        /// </summary>
        /// <remarks>
        /// Returns null when there are no more available tokens. e.g. you've reached the end or beginning of the stream.
        /// </remarks>
        public async Task<string?> NextAsync()
        {
            if (TryGetNextTokenOffset(out var nextTokenOffset) && _tokens.TryGetValue(nextTokenOffset!.Value, out var requestedToken))
            {
                return requestedToken.TokenValue;
            }

            await ParseNextBlockAsync();

            if (TryGetNextTokenOffset(out nextTokenOffset))
            {
                _currentByteOffset = nextTokenOffset!.Value;
                return _tokens[nextTokenOffset!.Value].TokenValue;
            }

            return null;
        }

        private async Task ParseNextBlockAsync()
        {
            // Continue to parse the stream into tokens
            byte[] buffer = new byte[_bufferSize];

            // Calculate the amount left to read.
            // This is the smaller of the buffer size and remaining data.
            var amountRemaining = ReadDirection == ReadDirection.Forward ? _stream.Length - _stream.Position : _stream.Position;
            int readSize = (int)Math.Min(_bufferSize, amountRemaining);

            // We've reached the end of the stream
            if (readSize == 0)
            {
                return;
            }

            // When reading a stream, we always go forwards.
            // Therefore when going backwards, seek back by the read size.
            // The stream position will be reset after reading.
            if (ReadDirection == ReadDirection.Backward)
            {
                _stream.Seek(-readSize, SeekOrigin.Current);
            }

            await _stream.ReadAsync(buffer.AsMemory(0, readSize));

            if (ReadDirection == ReadDirection.Backward)
            {
                _stream.Seek(-readSize, SeekOrigin.Current);
            }

            string bufferContent = Encoding.UTF8.GetString(buffer, 0, readSize);

            var tokens = bufferContent
                .SplitAndKeep(new[] { Constants.NewLine, Constants.Space })
                .ToArray();

            // We're going to record the offset for each token
            var offset = _stream.Position - readSize;

            // Last token may be incomplete. Insert all but last.
            var tokenIndexToIgnore = ReadDirection == ReadDirection.Forward
                ? tokens.Length - 1
                : 0;

            for (var i = 0; i < tokens.Length; i++)
            {
                if (i == tokenIndexToIgnore)
                {
                    continue;
                }

                var token = tokens[i];
                var prevToken = i > 0 ? tokens[i - 1] : null;
                var prevTokenOffset = prevToken != null ? offset - prevToken.Length : (long?)null;

                _tokens[offset] = new Token(prevTokenOffset, token);

                offset += token.Length;
            }

            // Move stream beyond unprocessed/incomplete token.
            _stream.Position += tokens[tokenIndexToIgnore].Length;
        }

        private bool TryGetNextTokenOffset(out long? offset)
        {
            // No processed tokens
            if (!_tokens.Any())
            {
                offset = ReadDirection == ReadDirection.Forward ? 0 : null;
                return offset != null;
            }

            //// First, get the current token
            //if (!_tokens.TryGetValue(_currentByteOffset, out var currentToken))
            //{
            //    offset = null;
            //    return false;
            //}

            // If we're reading forwards, the next offset is the current offset plus the current token length
            if (ReadDirection == ReadDirection.Forward && _tokens.TryGetValue(_currentByteOffset, out var currentToken))
            {
                offset = _currentByteOffset + currentToken.TokenValue.Length;
                return true;
            }

            // Handle the case where we're reading backwards and the stream is at the end.
            if (ReadDirection == ReadDirection.Backward && _currentByteOffset == _stream.Length)
            {
                // TODO
            }

            //offset = currentToken.PrevTokenOffset;
            offset = null;
            return false;// currentToken.PrevTokenOffset.HasValue;
        }

        public void Dispose()
        {
            ((IDisposable)_stream).Dispose();
        }

        private class Token
        {
            public Token(long? prevTokenOffset, string tokenValue)
            {
                PrevTokenOffset = prevTokenOffset;
                TokenValue = tokenValue;
            }

            public long? PrevTokenOffset { get; set; }
            public string TokenValue { get; }
        }
    }
}
