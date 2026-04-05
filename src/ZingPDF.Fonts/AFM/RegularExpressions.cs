using System.Text.RegularExpressions;

namespace ZingPDF.Fonts;

internal partial class RegularExpressions
{
    [GeneratedRegex(@"C (\-?\d+)\s*;")]
    public static partial Regex CharacterCode();

    [GeneratedRegex(@"WX (\d+)\s*;")]
    public static partial Regex CharacterWidth();
}
