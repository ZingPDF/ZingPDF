namespace ZingPDF.Text;

internal class FontProperties(int fontFlags)
{
    private readonly FontFlags _fontFlags = (FontFlags)fontFlags;

    public bool IsFixedPitch => _fontFlags.HasFlag(FontFlags.FixedPitch);
    public bool IsSerif => _fontFlags.HasFlag(FontFlags.Serif);
    public bool IsSymbolic => _fontFlags.HasFlag(FontFlags.Symbolic);
    public bool IsScript => _fontFlags.HasFlag(FontFlags.Script);
    public bool IsNonSymbolic => _fontFlags.HasFlag(FontFlags.NonSymbolic);
    public bool IsItalic => _fontFlags.HasFlag(FontFlags.Italic);
    public bool IsAllCap => _fontFlags.HasFlag(FontFlags.AllCap);
    public bool IsSmallCap => _fontFlags.HasFlag(FontFlags.SmallCap);
    public bool IsForceBold => _fontFlags.HasFlag(FontFlags.ForceBold);
}
