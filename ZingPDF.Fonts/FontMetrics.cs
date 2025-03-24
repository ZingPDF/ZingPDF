namespace ZingPDF.Fonts;

/// <summary>
/// Class to store font metrics information
/// </summary>
public class FontMetrics
{
    public string Name { get; set; }
    public int Ascent { get; set; }
    public int Descent { get; set; }
    public int? StandardHorizontalWidth { get; set; }
    public int? StandardVerticalWidth { get; set; }
    public int CapHeight { get; set; }
    public int XHeight { get; set; }
    public float ItalicAngle { get; set; }
    public bool IsFixedPitch { get; set; }
    public int? UnderlinePosition { get; set; }
    public int? UnderlineThickness { get; set; }
    public Dictionary<char, int> Widths { get; set; } = [];
    public Dictionary<(char, char), int> KerningPairs { get; set; } = [];

    /// <summary>
    /// Calculate the width of a string using these font metrics
    /// </summary>
    public double CalculateStringWidth(string text, double fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int width = 0;
        
        // Add character widths
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (Widths.TryGetValue(c, out int charWidth))
                width += charWidth;
            else
                width += Widths['J'];
        }
        
        // Apply kerning if available
        if (KerningPairs != null)
        {
            for (int i = 0; i < text.Length - 1; i++)
            {
                var pair = (text[i], text[i + 1]);
                if (KerningPairs.TryGetValue(pair, out int kerning))
                    width += kerning;
            }
        }
        
        // Scale by font size (AFM values are in 1/1000 of em)
        return width * fontSize / 1000;
    }
}
