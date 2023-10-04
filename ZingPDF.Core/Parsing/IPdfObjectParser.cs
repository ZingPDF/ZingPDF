using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal interface IPdfObjectParser<T> where T : PdfObject
    {
        T Parse(IEnumerable<string> tokens);
    }
}
