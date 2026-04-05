namespace ZingPDF.Text;

/// <summary>
/// Controls how text is encoded before it is written to a font resource.
/// </summary>
public enum FontTextEncoding
{
    /// <summary>
    /// Automatically choose a PDF text encoding.
    /// </summary>
    Auto,

    /// <summary>
    /// Encode text using the WinAnsi code page.
    /// </summary>
    WinAnsi
}
