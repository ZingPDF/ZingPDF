namespace ZingPDF.Text.Encoding.StandardEncoding;

public class StandardEncoding : System.Text.Encoding
{
    private static readonly char[] _codeToChar;
    private static readonly byte[] _charToCode;

    static StandardEncoding()
    {
        _codeToChar = new char[256];
        _charToCode = new byte[65536]; // UTF-16 space

        for (int i = 0; i < _charToCode.Length; i++)
            _charToCode[i] = 0x3F; // '?'

        // Default all bytes to U+FFFD (replacement character)
        for (int i = 0; i < _codeToChar.Length; i++)
            _codeToChar[i] = '\uFFFD';

        // Standard ASCII
        for (int i = 0x20; i <= 0x7E; i++)
            _codeToChar[i] = (char)i;

        // Special StandardEncoding mappings
        _codeToChar[0x18] = '\u02D8'; _codeToChar[0x19] = '\u02C7';
        _codeToChar[0x1A] = '\u02C6'; _codeToChar[0x1B] = '\u02D9';
        _codeToChar[0x1C] = '\u02DD'; _codeToChar[0x1D] = '\u02DB';
        _codeToChar[0x1E] = '\u02DA'; _codeToChar[0x1F] = '\u02DC';
        _codeToChar[0x80] = '\u2022'; _codeToChar[0x81] = '\u2020';
        _codeToChar[0x82] = '\u2021'; _codeToChar[0x83] = '\u2026';
        _codeToChar[0x84] = '\u2014'; _codeToChar[0x85] = '\u2013';
        _codeToChar[0x86] = '\u0192'; _codeToChar[0x87] = '\u2044';
        _codeToChar[0x88] = '\u2039'; _codeToChar[0x89] = '\u203A';
        _codeToChar[0x8A] = '\u2212'; _codeToChar[0x8B] = '\u2030';
        _codeToChar[0x8C] = '\u201E'; _codeToChar[0x8D] = '\u201C';
        _codeToChar[0x8E] = '\u201D'; _codeToChar[0x8F] = '\u2018';
        _codeToChar[0x90] = '\u2019'; _codeToChar[0x91] = '\u201A';
        _codeToChar[0x92] = '\u2122'; _codeToChar[0x93] = '\uFB01';
        _codeToChar[0x94] = '\uFB02'; _codeToChar[0x95] = '\u0141';
        _codeToChar[0x96] = '\u0152'; _codeToChar[0x97] = '\u0160';
        _codeToChar[0x98] = '\u0178'; _codeToChar[0x99] = '\u017D';
        _codeToChar[0x9A] = '\u0131'; _codeToChar[0x9B] = '\u0142';
        _codeToChar[0x9C] = '\u0153'; _codeToChar[0x9D] = '\u0161';
        _codeToChar[0x9E] = '\u017E'; _codeToChar[0xA0] = '\u20AC';

        // Latin-1 Supplement
        for (int i = 0xA1; i <= 0xFF; i++)
            _codeToChar[i] = (char)(0x00A0 + (i - 0xA0));

        // Build reverse lookup
        for (int i = 0; i < _codeToChar.Length; i++)
        {
            var ch = _codeToChar[i];
            if (ch != '\uFFFD')
                _charToCode[ch] = (byte)i;
        }
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        return count;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount,
                                 byte[] bytes, int byteIndex)
    {
        for (int i = 0; i < charCount; i++)
        {
            char ch = chars[charIndex + i];
            bytes[byteIndex + i] = _charToCode[ch];
        }
        return charCount;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        return count;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                 char[] chars, int charIndex)
    {
        for (int i = 0; i < byteCount; i++)
        {
            chars[charIndex + i] = _codeToChar[bytes[byteIndex + i]];
        }
        return byteCount;
    }

    public override int GetMaxByteCount(int charCount)
    {
        return charCount;
    }

    public override int GetMaxCharCount(int byteCount)
    {
        return byteCount;
    }
}
