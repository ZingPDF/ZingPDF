using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Objects.Streams;

internal class FilterConfig
{
    public required Name FilterName { get; init; }
    public required Dictionary DecodeParms { get; init; }
}
