using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.ObjectModel.Filters
{
    internal static class FilterFactory
    {
        public static IFilter Create(string name, Dictionary? decodeParams)
        {
            return name switch
            {
                Constants.Filters.ASCII85 => new ASCII85DecodeFilter(),
                Constants.Filters.ASCIIHex => new ASCIIHexDecodeFilter(),
                Constants.Filters.LZW => new LZWDecodeFilter(decodeParams),
                Constants.Filters.Flate => new FlateDecodeFilter(decodeParams),
                Constants.Filters.RunLength => new RunLengthDecodeFilter(),
                _ => throw new InvalidOperationException("Unsupported filter"),
            };
        }
    }
}
