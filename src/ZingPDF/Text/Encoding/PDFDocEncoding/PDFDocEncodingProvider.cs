using System.Text;

namespace ZingPDF.Text.Encoding.PDFDocEncoding;

public class PDFDocEncodingProvider : EncodingProvider
{
    private static readonly PDFDocEncodingProvider _instance = new();

    public static PDFDocEncodingProvider Instance => _instance;

    public override System.Text.Encoding? GetEncoding(string name)
    {
        if (string.Equals(name, PDFEncoding.PDFDoc, StringComparison.OrdinalIgnoreCase))
        {
            return new PDFDocEncoding();
        }

        return null;
    }

    public override System.Text.Encoding? GetEncoding(int codepage)
    {
        return null;
    }
}
