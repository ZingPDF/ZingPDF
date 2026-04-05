using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4
    /// 
    /// Provides internal logic for encoding and decoding streams.
    /// </summary>
    public interface IFilter
    {
        Name Name { get; }
        //FilterParams? Params { get; }
        Dictionary? Params { get; }
        MemoryStream Decode(Stream data);
        MemoryStream Encode(Stream data);
    }
}
