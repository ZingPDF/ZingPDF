namespace ZingPdf.Core.Objects.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4
    /// 
    /// Provides internal logic for encoding and decoding streams.
    /// </summary>
    internal interface IFilter
    {
        string Encode(byte[] data);
        byte[] Decode(string data);
    }
}
