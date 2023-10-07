using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal interface IPdfObjectParser<out T> where T : PdfObject
    {
        T Parse(IEnumerable<string> tokens);
    }
}
