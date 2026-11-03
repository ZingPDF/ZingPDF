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

        // Adobe PDF Reference, Appendix C: StandardEncoding is distinct from PDFDocEncoding.
        _codeToChar[0x27] = '\u2019';
        _codeToChar[0x60] = '\u2018';
        _codeToChar[0xA1] = '\u00A1';
        _codeToChar[0xA2] = '\u00A2';
        _codeToChar[0xA3] = '\u00A3';
        _codeToChar[0xA4] = '\u2044';
        _codeToChar[0xA5] = '\u00A5';
        _codeToChar[0xA6] = '\u0192';
        _codeToChar[0xA7] = '\u00A7';
        _codeToChar[0xA8] = '\u00A4';
        _codeToChar[0xA9] = '\u0027';
        _codeToChar[0xAA] = '\u201C';
        _codeToChar[0xAB] = '\u00AB';
        _codeToChar[0xAC] = '\u2039';
        _codeToChar[0xAD] = '\u203A';
        _codeToChar[0xAE] = '\uFB01';
        _codeToChar[0xAF] = '\uFB02';
        _codeToChar[0xB1] = '\u2013';
        _codeToChar[0xB2] = '\u2020';
        _codeToChar[0xB3] = '\u2021';
        _codeToChar[0xB4] = '\u00B7';
        _codeToChar[0xB6] = '\u00B6';
        _codeToChar[0xB7] = '\u2022';
        _codeToChar[0xB8] = '\u201A';
        _codeToChar[0xB9] = '\u201E';
        _codeToChar[0xBA] = '\u201D';
        _codeToChar[0xBB] = '\u00BB';
        _codeToChar[0xBC] = '\u2026';
        _codeToChar[0xBD] = '\u2030';
        _codeToChar[0xBF] = '\u00BF';
        _codeToChar[0xC1] = '\u0060';
        _codeToChar[0xC2] = '\u00B4';
        _codeToChar[0xC3] = '\u02C6';
        _codeToChar[0xC4] = '\u02DC';
        _codeToChar[0xC5] = '\u00AF';
        _codeToChar[0xC6] = '\u02D8';
        _codeToChar[0xC7] = '\u02D9';
        _codeToChar[0xC8] = '\u00A8';
        _codeToChar[0xCA] = '\u02DA';
        _codeToChar[0xCB] = '\u00B8';
        _codeToChar[0xCD] = '\u02DD';
        _codeToChar[0xCE] = '\u02DB';
        _codeToChar[0xCF] = '\u02C7';
        _codeToChar[0xD0] = '\u2014';
        _codeToChar[0xE1] = '\u00C6';
        _codeToChar[0xE3] = '\u00AA';
        _codeToChar[0xE8] = '\u0141';
        _codeToChar[0xE9] = '\u00D8';
        _codeToChar[0xEA] = '\u0152';
        _codeToChar[0xEB] = '\u00BA';
        _codeToChar[0xF1] = '\u00E6';
        _codeToChar[0xF5] = '\u0131';
        _codeToChar[0xF8] = '\u0142';
        _codeToChar[0xF9] = '\u00F8';
        _codeToChar[0xFA] = '\u0153';
        _codeToChar[0xFB] = '\u00DF';

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
