using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Syntax.Filters
{
    internal static class FilterFactory
    {
        public static IEnumerable<IFilter> CreateFilterInstances(IStreamDictionary dictionary)
        {
            if (dictionary.Filter is null)
            {
                yield break;
            }

            var filterNames = dictionary.Filter as ArrayObject ?? new[] { dictionary.Filter };

            var allFilterParamsArray = dictionary.DecodeParms as ArrayObject;

            var allFilterParams = allFilterParamsArray ??
                (dictionary.DecodeParms is Dictionary singleFilterParamsDictionary ? new[] { singleFilterParamsDictionary } : (ArrayObject?)null);

            for (var i = 0; i < filterNames.Count(); i++)
            {
                var filterName = (Name)filterNames.ElementAt(i);
                var filterParams = (Dictionary?)allFilterParams?.ElementAtOrDefault(i);

                yield return Create(filterName, filterParams);
            }
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
