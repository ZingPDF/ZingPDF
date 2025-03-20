namespace ZingPDF.Fonts;

using System;

/// <summary>
/// Exception thrown when a font is not found
/// </summary>
public class FontNotFoundException : Exception
{
    public FontNotFoundException(string message) : base(message) { }
}
