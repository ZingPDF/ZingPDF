namespace ZingPDF.Text.Encoding;

public static class PDFEncoding
{
    /// <summary>
    /// Standard Latin-text encoding. This is the built-in encoding defined in Type 1 Latin-text 
    /// font programs (but generally not in TrueType font programs). PDF processors shall not have 
    /// a predefined encoding named StandardEncoding. However, it is necessary to describe this 
    /// encoding, since a font’s built-in encoding can be used as the base encoding from which 
    /// differences may be specified in an encoding dictionary.
    /// </summary>
    public const string Standard = "StandardEncoding";

    /// <summary>
    /// Mac OS standard encoding for Latin text in Western writing systems. PDF processors shall 
    /// have a predefined encoding named MacRomanEncoding that may be used with both Type 1 and 
    /// TrueType fonts.
    /// </summary>
    public const string MacRoman = "MacRomanEncoding";

    /// <summary>
    /// Windows Code Page 1252, often called the "Windows ANSI" encoding. This is the standard Microsoft 
    /// specific encoding for Latin text in Western writing systems. PDF processors shall have a 
    /// predefined encoding named WinAnsiEncoding that may be used with both Type 1 and TrueType fonts.
    /// </summary>
    public const string WinAnsi = "WinAnsiEncoding";

    /// <summary>
    /// Encoding for text strings in a PDF document outside the document’s content streams. This is 
    /// one of two encodings (the other being Unicode) that may be used to represent text strings; 
    /// see 7.9.2.2, "Text string type". PDF does not have a predefined encoding named PDFDocEncoding; 
    /// it is not customary to use this encoding to show text from fonts.
    /// </summary>
    public const string PDFDoc = "PDFDocEncoding";

    /// <summary>
    /// An encoding for use with expert fonts — ones containing the expert character set. PDF processors 
    /// shall have a predefined encoding named MacExpertEncoding. Despite its name, it is not a 
    /// platform-specific encoding; however, only certain fonts have the appropriate character set for 
    /// use with this encoding. No such fonts are among the standard 14 predefined fonts.
    /// </summary>
    public const string MacExpert = "MacExpertEncoding";
}
