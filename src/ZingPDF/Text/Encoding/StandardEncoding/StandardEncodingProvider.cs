using System.Text;

namespace ZingPDF.Text.Encoding.StandardEncoding;

public class StandardEncodingProvider : EncodingProvider
{
    private static readonly StandardEncodingProvider _instance = new();

    public static StandardEncodingProvider Instance => _instance;

    public override System.Text.Encoding? GetEncoding(string name)
    {
        if (string.Equals(name, PDFEncoding.Standard, StringComparison.OrdinalIgnoreCase))
        {
            return new StandardEncoding();
        }

        return null;
    }

    public override System.Text.Encoding? GetEncoding(int codepage)
    {
        return null;
    }
}
