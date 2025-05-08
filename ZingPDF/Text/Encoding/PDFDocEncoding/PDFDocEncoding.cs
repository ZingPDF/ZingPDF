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
        [0xA0] = '\u00A0', // Non-breaking space
        [0xA1] = '\u00A1', // Inverted exclamation mark
        [0xA2] = '\u00A2', // Cent sign
        [0xA3] = '\u00A3', // Pound sign
        [0xA4] = '\u00A4', // Currency sign
        [0xA5] = '\u00A5', // Yen sign
        [0xA6] = '\u00A6', // Broken bar
        [0xA7] = '\u00A7', // Section sign
        [0xA8] = '\u00A8', // Diaeresis
        [0xA9] = '\u00A9', // Copyright sign
        [0xAA] = '\u00AA', // Feminine ordinal indicator
        [0xAB] = '\u00AB', // Left-pointing double angle quotation mark
        [0xAC] = '\u00AC', // Not sign
        [0xAD] = '\u00AD', // Soft hyphen
        [0xAE] = '\u00AE', // Registered sign
        [0xAF] = '\u00AF', // Macron

        [0xB0] = '\u00B0', // Degree sign
        [0xB1] = '\u00B1', // Plus-minus sign
        [0xB2] = '\u00B2', // Superscript two
        [0xB3] = '\u00B3', // Superscript three
        [0xB4] = '\u00B4', // Acute accent
        [0xB5] = '\u00B5', // Micro sign
        [0xB6] = '\u00B6', // Pilcrow sign
        [0xB7] = '\u00B7', // Middle dot
        [0xB8] = '\u00B8', // Cedilla
        [0xB9] = '\u00B9', // Superscript one
        [0xBA] = '\u00BA', // Masculine ordinal indicator
        [0xBB] = '\u00BB', // Right-pointing double angle quotation mark
        [0xBC] = '\u00BC', // Vulgar fraction one quarter
        [0xBD] = '\u00BD', // Vulgar fraction one half
        [0xBE] = '\u00BE', // Vulgar fraction three quarters
        [0xBF] = '\u00BF', // Inverted question mark

        [0xC0] = '\u00C0', // Latin capital letter A with grave
        [0xC1] = '\u00C1', // Latin capital letter A with acute
        [0xC2] = '\u00C2', // Latin capital letter A with circumflex
        [0xC3] = '\u00C3', // Latin capital letter A with tilde
        [0xC4] = '\u00C4', // Latin capital letter A with diaeresis
        [0xC5] = '\u00C5', // Latin capital letter A with ring above
        [0xC6] = '\u00C6', // Latin capital letter AE
        [0xC7] = '\u00C7', // Latin capital letter C with cedilla
        [0xC8] = '\u00C8', // Latin capital letter E with grave
        [0xC9] = '\u00C9', // Latin capital letter E with acute
        [0xCA] = '\u00CA', // Latin capital letter E with circumflex
        [0xCB] = '\u00CB', // Latin capital letter E with diaeresis
        [0xCC] = '\u00CC', // Latin capital letter I with grave
        [0xCD] = '\u00CD', // Latin capital letter I with acute
        [0xCE] = '\u00CE', // Latin capital letter I with circumflex
        [0xCF] = '\u00CF', // Latin capital letter I with diaeresis

        [0xD0] = '\u00D0', // Latin capital letter Eth
        [0xD1] = '\u00D1', // Latin capital letter N with tilde
        [0xD2] = '\u00D2', // Latin capital letter O with grave
        [0xD3] = '\u00D3', // Latin capital letter O with acute
        [0xD4] = '\u00D4', // Latin capital letter O with circumflex
        [0xD5] = '\u00D5', // Latin capital letter O with tilde
        [0xD6] = '\u00D6', // Latin capital letter O with diaeresis
        [0xD7] = '\u00D7', // Multiplication sign
        [0xD8] = '\u00D8', // Latin capital letter O with stroke
        [0xD9] = '\u00D9', // Latin capital letter U with grave
        [0xDA] = '\u00DA', // Latin capital letter U with acute
        [0xDB] = '\u00DB', // Latin capital letter U with circumflex
        [0xDC] = '\u00DC', // Latin capital letter U with diaeresis
        [0xDD] = '\u00DD', // Latin capital letter Y with acute
        [0xDE] = '\u00DE', // Latin capital letter Thorn
        [0xDF] = '\u00DF', // Latin small letter sharp s

        [0xE0] = '\u00E0', // Latin small letter a with grave
        [0xE1] = '\u00E1', // Latin small letter a with acute
        [0xE2] = '\u00E2', // Latin small letter a with circumflex
        [0xE3] = '\u00E3', // Latin small letter a with tilde
        [0xE4] = '\u00E4', // Latin small letter a with diaeresis
        [0xE5] = '\u00E5', // Latin small letter a with ring above
        [0xE6] = '\u00E6', // Latin small letter ae
        [0xE7] = '\u00E7', // Latin small letter c with cedilla
        [0xE8] = '\u00E8', // Latin small letter e with grave
        [0xE9] = '\u00E9', // Latin small letter e with acute
        [0xEA] = '\u00EA', // Latin small letter e with circumflex
        [0xEB] = '\u00EB', // Latin small letter e with diaeresis
        [0xEC] = '\u00EC', // Latin small letter i with grave
        [0xED] = '\u00ED', // Latin small letter i with acute
        [0xEE] = '\u00EE', // Latin small letter i with circumflex
        [0xEF] = '\u00EF', // Latin small letter i with diaeresis

        [0xF0] = '\u00F0', // Latin small letter eth
        [0xF1] = '\u00F1', // Latin small letter n with tilde
        [0xF2] = '\u00F2', // Latin small letter o with grave
        [0xF3] = '\u00F3', // Latin small letter o with acute
        [0xF4] = '\u00F4', // Latin small letter o with circumflex
        [0xF5] = '\u00F5', // Latin small letter o with tilde
        [0xF6] = '\u00F6', // Latin small letter o with diaeresis
        [0xF7] = '\u00F7', // Division sign
        [0xF8] = '\u00F8', // Latin small letter o with stroke
        [0xF9] = '\u00F9', // Latin small letter u with grave
        [0xFA] = '\u00FA', // Latin small letter u with acute
        [0xFB] = '\u00FB', // Latin small letter u with circumflex
        [0xFC] = '\u00FC', // Latin small letter u with diaeresis
        [0xFD] = '\u00FD', // Latin small letter y with acute
        [0xFE] = '\u00FE', // Latin small letter thorn
        [0xFF] = '\u00FF'  // Latin small letter y with diaeresis
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
        ArgumentNullException.ThrowIfNull(chars);

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
                // Every byte should map to a character, in dev, throw.
                // TODO: in prod, replace with the replacement character '\uFFFD'
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
