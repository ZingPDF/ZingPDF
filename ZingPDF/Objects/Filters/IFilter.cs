using ZingPDF.Objects.Primitives;

namespace ZingPDF.Objects.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4
    /// 
    /// Provides internal logic for encoding and decoding streams.
    /// </summary>
    internal interface IFilter
    {
        Name Name { get; }
        //FilterParams? Params { get; }
        Dictionary? Params { get; }
        byte[] Decode(byte[] data);
        byte[] Encode(byte[] data);
    }
}
