using System.Text.RegularExpressions;

namespace ZingPDF.Parsing;

internal partial class RegularExpressions
{
    [GeneratedRegex(@"^\%PDF-")] // %PDF-2.0
    public static partial Regex Header();

    [GeneratedRegex(@"^-?\d+\s*")] // 1234
    public static partial Regex Integer();

    [GeneratedRegex(@"^-?\d*\.\d+")] // 595.276000
    public static partial Regex RealNumber();

    [GeneratedRegex(@"^\s*\/.+")]  // /Name
    public static partial Regex Name();

    [GeneratedRegex(@"^[\d]+ [\d]+ obj")]  // 1 0 obj
    public static partial Regex IndirectObject();

    [GeneratedRegex(@"^[\d]+ [\d]+ R")]  // 49 0 R
    public static partial Regex IndirectObjectReference();

    [GeneratedRegex(@"^[0-9]+\s[0-9]+\s[fn]")]  // 0000000000 65535 f
    public static partial Regex CrossReferenceEntry();

    [GeneratedRegex(@"^\(D:\d{4,14}[+\-Z]\d{2}'?\d{2}'?\)")]  // (D:20230922161207+10'00')
    public static partial Regex Date();
}
