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
    internal class TokenReverseStreamReader : IDisposable
    {
        private readonly int _bufferSize = 1024;
        private readonly Stream _stream;

        private readonly Dictionary<long, Token> _tokens = new();

        public TokenReverseStreamReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));     
            if (!stream.CanSeek) throw new ArgumentException("Stream must be seekable", nameof(stream));

            _stream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// Get the next token in the stream
        /// </summary>
        /// <remarks>
        /// Returns null when there are no more available tokens. e.g. you've reached the beginning of the stream.
        /// </remarks>
        public async Task<string?> NextAsync()
        {
            if (TryMoveStreamToNextToken())
            {
                return _tokens[_stream.Position].TokenValue;
            }

            await ParseNextBlockAsync();

            if (_tokens.TryGetValue(_stream.Position, out var token)) {
                return token.TokenValue;
            }

            return null;
        }

        private async Task ParseNextBlockAsync()
        {
            // Continue to parse the stream into tokens
            byte[] buffer = new byte[_bufferSize];

            // Calculate the amount left to read.
            // This is the smaller of the buffer size and remaining data.
            int readSize = (int)Math.Min(_bufferSize, _stream.Position);

            // We've reached the end of the stream
            if (readSize == 0)
            {
                return;
            }

            // When reading a stream, we always go forwards.
            // Therefore when going backwards, seek back by the read size.
            // The stream position will be reset after reading.
            _stream.Seek(-readSize, SeekOrigin.Current);

            await _stream.ReadAsync(buffer.AsMemory(0, readSize));

            _stream.Seek(-readSize, SeekOrigin.Current);

            string bufferContent = Encoding.UTF8.GetString(buffer, 0, readSize);

            var tokens = bufferContent
                .SplitAndKeep(new[] { Constants.NewLine, Constants.Space })
                .ToArray();

            // We're going to record the offset for each token
            var startIndex = 0;

            // First token may be incomplete unless we've reached the beginning of the stream.
            if (_stream.Position > 0)
            {
                startIndex = 1;
                var incompleteTokenLength = tokens[0].Length;

                _stream.Position += incompleteTokenLength;
            }
            
            for (var i = startIndex; i < tokens.Length; i++)
            {
                var token = tokens[i];
                var prevToken = i > startIndex ? tokens[i - 1] : null;
                var prevTokenOffset = prevToken != null ? _stream.Position - prevToken.Length : (long?)null;

                Console.WriteLine($"Parsed token at position: {_stream.Position}. Value: {token}");
                _tokens[_stream.Position] = new Token(prevTokenOffset, token);

                _stream.Position += token.Length;
            }

            _stream.Position -= tokens.Last().Length;
        }

        private bool TryMoveStreamToNextToken()
        {
            // No processed tokens
            if (!_tokens.Any())
            {
                return false;
            }

            // Handle the case where the stream is at the end.
            if (_stream.Position == _stream.Length)
            {
                _stream.Position -= _tokens.Last().Value.TokenValue.Length;
                return true;
            }

            if (_tokens.TryGetValue(_stream.Position, out var token) && token.PrevTokenOffset.HasValue)
            {
                _stream.Position = token.PrevTokenOffset.Value;
                return true;
            }

            return false;
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
