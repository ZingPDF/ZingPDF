using System.Text;

namespace ZingPDF.Text.Encoding.PDFDocEncoding;

/// <summary>
/// Encoding class for PDFDocEncoding as defined in the PDF specification for text strings.
/// Implements a single-byte encoding mapping between bytes and Unicode characters.
/// </summary>
public sealed class PDFDocEncoding : System.Text.Encoding
{
    // Mapping from byte values to Unicode characters
    private static readonly Dictionary<byte,char> CodeToUnicode = new Dictionary<byte,char>
    {
        // 0x00–0x1F: control codes if you need them (usually not printed)
        [0x20] = '\u0020', // space
        [0x21] = '\u0021', [0x22] = '\u0022', [0x23] = '\u0023',
        [0x24] = '\u0024', [0x25] = '\u0025', [0x26] = '\u0026',
        [0x27] = '\u0027', [0x28] = '\u0028', [0x29] = '\u0029',
        [0x2A] = '\u002A', [0x2B] = '\u002B', [0x2C] = '\u002C',
        [0x2D] = '\u002D', [0x2E] = '\u002E', [0x2F] = '\u002F',
        [0x30] = '\u0030', [0x31] = '\u0031', [0x32] = '\u0032',
        [0x33] = '\u0033', [0x34] = '\u0034', [0x35] = '\u0035',
        [0x36] = '\u0036', [0x37] = '\u0037', [0x38] = '\u0038',
        [0x39] = '\u0039', [0x3A] = '\u003A', [0x3B] = '\u003B',
        [0x3C] = '\u003C', [0x3D] = '\u003D', [0x3E] = '\u003E',
        [0x3F] = '\u003F', [0x40] = '\u0040', [0x41] = '\u0041',
        [0x42] = '\u0042', [0x43] = '\u0043', [0x44] = '\u0044',
        [0x45] = '\u0045', [0x46] = '\u0046', [0x47] = '\u0047',
        [0x48] = '\u0048', [0x49] = '\u0049', [0x4A] = '\u004A',
        [0x4B] = '\u004B', [0x4C] = '\u004C', [0x4D] = '\u004D',
        [0x4E] = '\u004E', [0x4F] = '\u004F', [0x50] = '\u0050',
        [0x51] = '\u0051', [0x52] = '\u0052', [0x53] = '\u0053',
        [0x54] = '\u0054', [0x55] = '\u0055', [0x56] = '\u0056',
        [0x57] = '\u0057', [0x58] = '\u0058', [0x59] = '\u0059',
        [0x5A] = '\u005A', [0x5B] = '\u005B', [0x5C] = '\u005C',
        [0x5D] = '\u005D', [0x5E] = '\u005E', [0x5F] = '\u005F',
        [0x60] = '\u0060', [0x61] = '\u0061', [0x62] = '\u0062',
        [0x63] = '\u0063', [0x64] = '\u0064', [0x65] = '\u0065',
        [0x66] = '\u0066', [0x67] = '\u0067', [0x68] = '\u0068',
        [0x69] = '\u0069', [0x6A] = '\u006A', [0x6B] = '\u006B',
        [0x6C] = '\u006C', [0x6D] = '\u006D', [0x6E] = '\u006E',
        [0x6F] = '\u006F', [0x70] = '\u0070', [0x71] = '\u0071',
        [0x72] = '\u0072', [0x73] = '\u0073', [0x74] = '\u0074',
        [0x75] = '\u0075', [0x76] = '\u0076', [0x77] = '\u0077',
        [0x78] = '\u0078', [0x79] = '\u0079', [0x7A] = '\u007A',
        [0x7B] = '\u007B', [0x7C] = '\u007C', [0x7D] = '\u007D',
        [0x7E] = '\u007E',

        // Extended PDFDocEncoding (0x80–0x9F):
        [0x80] = '\u2022', // bullet
        [0x81] = '\u2020', // dagger
        [0x82] = '\u2021', // double dagger
        [0x83] = '\u2026', // ellipsis
        [0x84] = '\u2014', // em dash
        [0x85] = '\u2013', // en dash
        [0x86] = '\u0192', // latin small f with hook
        [0x87] = '\u2044', // fraction slash
        [0x88] = '\u2039', // single left-pointing angle quote
        [0x89] = '\u203A', // single right-pointing angle quote
        [0x8A] = '\u2212', // minus sign
        [0x8B] = '\u2030', // per mille sign
        [0x8C] = '\u201E', // double low-9 quotation mark
        [0x8D] = '\u201C', // left double quotation mark
        [0x8E] = '\u201D', // right double quotation mark
        [0x8F] = '\u2018', // left single quotation mark
        [0x90] = '\u2019', // right single quotation mark
        [0x91] = '\u201A', // single low-9 quotation mark
        [0x92] = '\u201C', // left double quotation mark (variant)
        [0x93] = '\u201D', // right double quotation mark (variant)
        [0x94] = '\u201F', // double high-reversed-9 quotation mark
        [0x95] = '\u2026', // ellipsis
        [0x96] = '\u2014', // em dash
        [0x97] = '\u2013', // en dash
        [0x98] = '\u0192', // latin small f with hook
        [0x99] = '\u2044', // fraction slash
        [0x9A] = '\u2039', // single left-pointing angle quote
        [0x9B] = '\u203A', // single right-pointing angle quote
        [0x9C] = '\u2212', // minus sign
        [0x9D] = '\u2030', // per mille sign
        [0x9E] = '\u00A0', // no-break space
        [0x9F] = '\u00A1', // inverted exclamation mark

        // 0xA0–0xFF: same as ISO-8859-1
        [0xA0] = '\u00A0', [0xA1] = '\u00A1', [0xA2] = '\u00A2',
        /* … through … */
        [0xFE] = '\u00FE', [0xFF] = '\u00FF'
    };

    // Reverse mapping from Unicode characters to byte values
    private static readonly Dictionary<char, byte> UnicodeToCode;

    static PDFDocEncoding()
    {
        UnicodeToCode = new Dictionary<char, byte>(CodeToUnicode.Count);
        foreach (var kv in CodeToUnicode)
        {
            UnicodeToCode[kv.Value] = kv.Key;
        }
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        if (chars == null) throw new ArgumentNullException(nameof(chars));
        if (index < 0 || count < 0 || index + count > chars.Length) throw new ArgumentOutOfRangeException();
        return count;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount,
                                  byte[] bytes, int byteIndex)
    {
        if (chars == null) throw new ArgumentNullException(nameof(chars));
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (charIndex < 0 || charCount < 0 || charIndex + charCount > chars.Length) throw new ArgumentOutOfRangeException();
        if (byteIndex < 0 || byteIndex + charCount > bytes.Length) throw new ArgumentOutOfRangeException();

        for (int i = 0; i < charCount; i++)
        {
            char c = chars[charIndex + i];
            if (!UnicodeToCode.TryGetValue(c, out byte b))
            {
                throw new EncoderFallbackException($"Character '{c}' cannot be encoded in PDFDocEncoding.");
            }
            bytes[byteIndex + i] = b;
        }
        return charCount;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (index < 0 || count < 0 || index + count > bytes.Length) throw new ArgumentOutOfRangeException();
        return count;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount,
                                  char[] chars, int charIndex)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (chars == null) throw new ArgumentNullException(nameof(chars));
        if (byteIndex < 0 || byteCount < 0 || byteIndex + byteCount > bytes.Length) throw new ArgumentOutOfRangeException();
        if (charIndex < 0 || charIndex + byteCount > chars.Length) throw new ArgumentOutOfRangeException();

        for (int i = 0; i < byteCount; i++)
        {
            byte b = bytes[byteIndex + i];
            if (!CodeToUnicode.TryGetValue(b, out char c))
            {
                // Undefined byte: preserve raw value as Unicode code point
                c = (char)b;
            }
            chars[charIndex + i] = c;
        }
        return byteCount;
    }

    public override int GetMaxByteCount(int charCount)
    {
        if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));
        return charCount;
    }

    public override int GetMaxCharCount(int byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        return byteCount;
    }

    /// <summary>
    /// PDFDocEncoding does not use a preamble (BOM).
    /// </summary>
    public override byte[] GetPreamble() => Array.Empty<byte>();

    public override string BodyName => "PDFDocEncoding";
    public override string EncodingName => "PDF Doc Encoding";
    public override string HeaderName => null;
    public override string WebName => "pdf-doc";
    public override int WindowsCodePage => 1252;
}
