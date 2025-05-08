using ZingPDF.Parsing.Parsers;

namespace ZingPDF;

public class PdfContext : IPdfContext
{
    public PdfContext(Stream stream)
    {
        Parser = new Parser(this);
        Objects = new PdfObjectCollection(stream, this);
    }

    public Parser Parser { get; }
    public IPdfObjectCollection Objects { get; }
}
