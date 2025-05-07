using ZingPDF.Parsing.Parsers;

namespace ZingPDF
{
    public interface IPdfContext
    {
        IPdfObjectCollection Objects { get; }
        Parser Parser { get; }
    }
}