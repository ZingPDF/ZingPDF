using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4
    /// 
    /// Provides internal logic for encoding and decoding streams.
    /// </summary>
    internal interface IFilter
    {
        Name Name { get; }
        FilterParams? Params { get; }
        byte[] Decode(byte[] data);
        byte[] Encode(byte[] data);
    }
}
