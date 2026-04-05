using System.Text.RegularExpressions;

namespace ZingPDF;

internal partial class RegularExpressions
{
    [GeneratedRegex(@"#([0-9A-Fa-f]{2})")]
    public static partial Regex TwoDigitHexCode();
}
