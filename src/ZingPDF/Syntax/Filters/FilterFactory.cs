using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters
{
    internal static class FilterFactory
    {
        public static IEnumerable<IFilter> CreateFilterInstances(
           IEnumerable<Name> filterNames,
           IEnumerable<Dictionary> allFilterParams
           )
        {
            List<IFilter> filters = [];

            for (var i = 0; i < filterNames.Count(); i++)
            {
                var filterName = filterNames.ElementAt(i);
                var filterParams = allFilterParams.ElementAtOrDefault(i);

                filters.Add(Create(filterName, filterParams));
            }

            return filters;
        }

        public static IFilter Create(string name, Dictionary? decodeParams)
        {
            return name switch
            {
                Constants.Filters.ASCII85 => new ASCII85DecodeFilter(),
                Constants.Filters.ASCIIHex => new ASCIIHexDecodeFilter(),
                Constants.Filters.LZW => new LZWDecodeFilter(decodeParams),
                Constants.Filters.Flate => new FlateDecodeFilter(decodeParams),
                Constants.Filters.RunLength => new RunLengthDecodeFilter(),
                Constants.Filters.DCT => new DCTDecodeFilter(decodeParams),
                Constants.Filters.JPX => new JPXDecodeFilter(),
                _ => throw new InvalidOperationException("Unsupported filter"),
            };
        }
    }
}
