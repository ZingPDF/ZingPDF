namespace ZingPDF.Text.Encoding
{
    /// <summary>
    /// A single-byte PDF encoding built from a base encoding and a differences map.
    /// </summary>
    public class DerivedEncoding : System.Text.Encoding
    {
        private readonly System.Text.Encoding _baseEncoding;
        private readonly Dictionary<byte, char> _differences;

        public DerivedEncoding(System.Text.Encoding baseEncoding, IDictionary<byte, char> differences)
        {
            _baseEncoding = baseEncoding ?? throw new ArgumentNullException(nameof(baseEncoding));
            _differences = new Dictionary<byte, char>(differences ?? throw new ArgumentNullException(nameof(differences)));
        }

        public override char[] GetChars(byte[] bytes, int index, int count)
        {
            var chars = _baseEncoding.GetChars(bytes, index, count);
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[index + i];
                if (_differences.TryGetValue(b, out var dch))
                {
                    chars[i] = dch;
                }
            }
            return chars;
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            var chars = GetChars(bytes, index, count);
            return new string(chars);
        }

        public override int GetByteCount(char[] chars, int index, int count)
            => _baseEncoding.GetByteCount(chars, index, count);

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            => _baseEncoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

        public override int GetCharCount(byte[] bytes, int index, int count)
            => count;

        public override int GetMaxByteCount(int charCount)
            => _baseEncoding.GetMaxByteCount(charCount);

        public override int GetMaxCharCount(int byteCount)
            => _baseEncoding.GetMaxCharCount(byteCount);

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            char[] temp = GetChars(bytes, byteIndex, byteCount);
            Array.Copy(temp, 0, chars, charIndex, temp.Length);
            return temp.Length;
        }
    }
}