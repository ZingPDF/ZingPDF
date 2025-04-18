using System.Text;

namespace ZingPDF.Syntax.CommonDataStructures.Strings;

public class PdfDocEncodingProvider : EncodingProvider
{
    public override Encoding GetEncoding(string name)
    {
        if (string.Equals(name, "pdfdocencoding", StringComparison.OrdinalIgnoreCase))
        {
            return new PdfDocEncoding();
        }

        return null;
    }

    public override Encoding GetEncoding(int codepage)
    {
        // you could even map a custom codepage number, e.g. 57005
        if (codepage == 57005)
        {
            return new PdfDocEncoding();
        }

        return null;
    }
}
