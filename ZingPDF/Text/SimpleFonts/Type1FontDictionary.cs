using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Text.SimpleFonts
{
    internal class Type1FontDictionary : FontDictionary
    {
        public Type1FontDictionary(Name baseFont, Name? name = null) : base(Subtypes.Type1)
        {
            ArgumentNullException.ThrowIfNull(baseFont);

            Set(Constants.DictionaryKeys.Font.Type1.BaseFont, baseFont);

            if (name is not null)
            {
                Set(Constants.DictionaryKeys.Font.Type1.Name, name);
            }
        }

        /// <summary>
        /// (Required in PDF 1.0; optional in PDF 1.1 through 1.7, deprecated in PDF 2.0)
        /// The name by which this font is referenced in the Font subdictionary of the current resource dictionary.
        /// </summary>
        public AsyncProperty<Name>? Name => Get<Name>(Constants.DictionaryKeys.Font.Type1.Name);

        /// <summary>
        /// (Required) The PostScript language name of the font. For Type 1 fonts, this is always the value 
        /// of the FontName entry in the font program; for more information, see Section 5.2 of the 
        /// PostScript Language Reference, Third Edition. The PostScript language name of the font may 
        /// be used to find the font program in the PDF processor or its environment. It is also the 
        /// name that is used when printing to a PostScript language compatible output device.
        /// </summary>
        public AsyncProperty<Name> BaseFont => Get<Name>(Constants.DictionaryKeys.Font.Type1.BaseFont)!;

        /// <summary>
        /// (Required; optional in PDF 1.0-1.7 for the standard 14 fonts) The first character code defined 
        /// in the font’s Widths array.
        /// </summary>
        public AsyncProperty<Integer>? FirstChar => Get<Integer>(Constants.DictionaryKeys.Font.Type1.FirstChar);

        /// <summary>
        /// (Required; optional in PDF 1.0-1.7 for the standard 14 fonts) The last character code 
        /// defined in the font’s Widths array.
        /// </summary>
        public AsyncProperty<Integer>? LastChar => Get<Integer>(Constants.DictionaryKeys.Font.Type1.LastChar);

        /// <summary>
        /// (Required; optional in PDF 1.0-1.7 for the standard 14 fonts; indirect reference preferred) 
        /// An array of (LastChar - FirstChar + 1) numbers, each element being the glyph width for the 
        /// character code that equals FirstChar plus the array index. For character codes outside the 
        /// range FirstChar to LastChar, the value of MissingWidth from the FontDescriptor entry for 
        /// this font shall be used. The glyph widths shall be measured in units in which 1000 units 
        /// correspond to 1 unit in text space. These widths shall be consistent with the actual widths 
        /// given in the font program. For more information on glyph widths and other glyph metrics, 
        /// see 9.2.4, "Glyph positioning and metrics".
        /// </summary>
        public AsyncProperty<ArrayObject>? Widths => Get<ArrayObject>(Constants.DictionaryKeys.Font.Type1.Widths);

        /// <summary>
        /// (Required; optional in PDF 1.0-1.7 for the standard 14 fonts; shall be an indirect reference) 
        /// A font descriptor describing the font’s metrics other than its glyph widths (see 9.8, "Font descriptors").
        /// For the standard 14 fonts, the entries FirstChar, LastChar, Widths, and FontDescriptor shall either all 
        /// be present or all be absent. Ordinarily, these dictionary keys may be absent; specifying them enables a 
        /// standard font to be overridden; see 9.6.2.2, "Standard Type 1 fonts (standard 14 fonts) (PDF 1.0-1.7)".
        /// </summary>
        public AsyncProperty<Dictionary>? FontDescriptor => Get<Dictionary>(Constants.DictionaryKeys.Font.Type1.FontDescriptor);

        /// <summary>
        /// (Optional) A specification of the font’s character encoding if different from its built-in encoding. 
        /// The value of Encoding shall be either the name of a predefined encoding (MacRomanEncoding, 
        /// MacExpertEncoding, or WinAnsiEncoding, as described in Annex D, "Character sets and encodings") 
        /// or an encoding dictionary that shall specify differences from the font’s built-in encoding or from 
        /// a specified predefined encoding (see 9.6.5, "Character encoding").
        /// </summary>
        public AsyncProperty<IPdfObject>? Encoding => Get<IPdfObject>(Constants.DictionaryKeys.Font.Type1.Encoding);

        /// <summary>
        /// (Optional; PDF 1.2) A stream containing a CMap file that maps character codes to Unicode values 
        /// (see 9.10.3, "ToUnicode CMaps").
        /// </summary>
        // TODO: implement CMapStreamDictionary
        public AsyncProperty<StreamObject<StreamDictionary>>? ToUnicode => Get<StreamObject<StreamDictionary>>(Constants.DictionaryKeys.Font.Type1.ToUnicode); 
    }
}
