using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZingPDF.Text.Encoding;

public static class GlyphToUnicodeTranslator
{
    // Handle standard Adobe glyph names
    private static readonly Dictionary<string, char> _standardGlyphMap = new(StringComparer.Ordinal)
    {
        // Basic Latin
        {"space", '\u0020'}, {"exclam", '\u0021'}, {"quotedbl", '\u0022'}, {"numbersign", '\u0023'},
        {"dollar", '\u0024'}, {"percent", '\u0025'}, {"ampersand", '\u0026'}, {"quotesingle", '\u0027'},
        {"parenleft", '\u0028'}, {"parenright", '\u0029'}, {"asterisk", '\u002A'}, {"plus", '\u002B'},
        {"comma", '\u002C'}, {"hyphen", '\u002D'}, {"period", '\u002E'}, {"slash", '\u002F'},
        
        // Numbers
        {"zero", '\u0030'}, {"one", '\u0031'}, {"two", '\u0032'}, {"three", '\u0033'},
        {"four", '\u0034'}, {"five", '\u0035'}, {"six", '\u0036'}, {"seven", '\u0037'},
        {"eight", '\u0038'}, {"nine", '\u0039'},
        
        // More punctuation
        {"colon", '\u003A'}, {"semicolon", '\u003B'}, {"less", '\u003C'}, {"equal", '\u003D'},
        {"greater", '\u003E'}, {"question", '\u003F'}, {"at", '\u0040'},
        
        // Uppercase Latin
        {"A", '\u0041'}, {"B", '\u0042'}, {"C", '\u0043'}, {"D", '\u0044'}, {"E", '\u0045'},
        {"F", '\u0046'}, {"G", '\u0047'}, {"H", '\u0048'}, {"I", '\u0049'}, {"J", '\u004A'},
        {"K", '\u004B'}, {"L", '\u004C'}, {"M", '\u004D'}, {"N", '\u004E'}, {"O", '\u004F'},
        {"P", '\u0050'}, {"Q", '\u0051'}, {"R", '\u0052'}, {"S", '\u0053'}, {"T", '\u0054'},
        {"U", '\u0055'}, {"V", '\u0056'}, {"W", '\u0057'}, {"X", '\u0058'}, {"Y", '\u0059'},
        {"Z", '\u005A'},
        
        // More punctuation
        {"bracketleft", '\u005B'}, {"backslash", '\u005C'}, {"bracketright", '\u005D'},
        {"asciicircum", '\u005E'}, {"underscore", '\u005F'}, {"grave", '\u0060'},
        
        // Lowercase Latin
        {"a", '\u0061'}, {"b", '\u0062'}, {"c", '\u0063'}, {"d", '\u0064'}, {"e", '\u0065'},
        {"f", '\u0066'}, {"g", '\u0067'}, {"h", '\u0068'}, {"i", '\u0069'}, {"j", '\u006A'},
        {"k", '\u006B'}, {"l", '\u006C'}, {"m", '\u006D'}, {"n", '\u006E'}, {"o", '\u006F'},
        {"p", '\u0070'}, {"q", '\u0071'}, {"r", '\u0072'}, {"s", '\u0073'}, {"t", '\u0074'},
        {"u", '\u0075'}, {"v", '\u0076'}, {"w", '\u0077'}, {"x", '\u0078'}, {"y", '\u0079'},
        {"z", '\u007A'},
        
        // Final punctuation
        {"braceleft", '\u007B'}, {"bar", '\u007C'}, {"braceright", '\u007D'}, {"asciitilde", '\u007E'},
        
        // Common special characters
        {"bullet", '\u2022'}, {"dagger", '\u2020'}, {"daggerdbl", '\u2021'}, {"ellipsis", '\u2026'},
        {"emdash", '\u2014'}, {"endash", '\u2013'}, {"euro", '\u20AC'}, {"trademark", '\u2122'},
        {"quotedblleft", '\u201C'}, {"quotedblright", '\u201D'}, {"quoteleft", '\u2018'}, {"quoteright", '\u2019'},
        
        // Accented characters
        {"Aacute", '\u00C1'}, {"aacute", '\u00E1'}, {"Acircumflex", '\u00C2'}, {"acircumflex", '\u00E2'},
        {"Adieresis", '\u00C4'}, {"adieresis", '\u00E4'}, {"Agrave", '\u00C0'}, {"agrave", '\u00E0'},
        {"Aring", '\u00C5'}, {"aring", '\u00E5'}, {"Atilde", '\u00C3'}, {"atilde", '\u00E3'},
        {"Ccedilla", '\u00C7'}, {"ccedilla", '\u00E7'}, {"Eacute", '\u00C9'}, {"eacute", '\u00E9'},
        {"Ecircumflex", '\u00CA'}, {"ecircumflex", '\u00EA'}, {"Edieresis", '\u00CB'}, {"edieresis", '\u00EB'},
        {"Egrave", '\u00C8'}, {"egrave", '\u00E8'}, {"Iacute", '\u00CD'}, {"iacute", '\u00ED'},
        {"Icircumflex", '\u00CE'}, {"icircumflex", '\u00EE'}, {"Idieresis", '\u00CF'}, {"idieresis", '\u00EF'},
        {"Igrave", '\u00CC'}, {"igrave", '\u00EC'}, {"Ntilde", '\u00D1'}, {"ntilde", '\u00F1'},
        {"Oacute", '\u00D3'}, {"oacute", '\u00F3'}, {"Ocircumflex", '\u00D4'}, {"ocircumflex", '\u00F4'},
        {"Odieresis", '\u00D6'}, {"odieresis", '\u00F6'}, {"Ograve", '\u00D2'}, {"ograve", '\u00F2'},
        {"Otilde", '\u00D5'}, {"otilde", '\u00F5'}, {"Scaron", '\u0160'}, {"scaron", '\u0161'},
        {"Uacute", '\u00DA'}, {"uacute", '\u00FA'}, {"Ucircumflex", '\u00DB'}, {"ucircumflex", '\u00FB'},
        {"Udieresis", '\u00DC'}, {"udieresis", '\u00FC'}, {"Ugrave", '\u00D9'}, {"ugrave", '\u00F9'},
        {"Ydieresis", '\u0178'}, {"ydieresis", '\u00FF'}, {"Zcaron", '\u017D'}, {"zcaron", '\u017E'},
        
        // Ligatures
        {"fi", '\uFB01'}, {"fl", '\uFB02'}, {"OE", '\u0152'}, {"oe", '\u0153'},
        
        // Symbols
        {"copyright", '\u00A9'}, {"registered", '\u00AE'}, {"degree", '\u00B0'}, {"plusminus", '\u00B1'},
        {"multiply", '\u00D7'}, {"divide", '\u00F7'}, {"lozenge", '\u25CA'}, {"section", '\u00A7'},
        {"paragraph", '\u00B6'}, {"mu", '\u00B5'}, {"sterling", '\u00A3'}, {"yen", '\u00A5'}
        
        // Add more mappings as needed
    };

    public static char Translate(string glyphName)
    {
        // Check for standard mappings
        if (_standardGlyphMap.TryGetValue(glyphName, out char unicodeChar))
        {
            return unicodeChar;
        }

        // Handle uniXXXX names (direct Unicode values)
        if (glyphName.StartsWith("uni") && glyphName.Length > 3)
        {
            string hexValue = glyphName[3..];
            if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
            {
                return (char)codePoint;
            }
        }

        // Handle uXXXX, uXXXXX names (another Unicode notation)
        if (glyphName.StartsWith('u') && glyphName.Length > 1)
        {
            string hexValue = glyphName[1..];
            if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
            {
                return (char)codePoint;
            }
        }

        // Handle special notations like 'a_b' or 'uni0041_0042'
        if (glyphName.Contains('_'))
        {
            // For combined characters, return the first one
            return Translate(glyphName.Split('_')[0]);
        }

        // Handle notations like 'A.swash', 'a.alt', etc.
        if (glyphName.Contains('.'))
        {
            // Return the base character
            return Translate(glyphName.Split('.')[0]);
        }

        // Return a default character for unknown glyph names
        // You might want to log these for debugging
        Console.WriteLine($"Unknown glyph name: {glyphName}");
        return '?';
    }
}
