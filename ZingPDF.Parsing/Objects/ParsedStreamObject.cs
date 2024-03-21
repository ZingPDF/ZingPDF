using ZingPDF.Extensions;
using ZingPDF.Objects.Filters;

namespace ZingPDF.Objects.Primitives.Streams
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8 - Stream objects.<para></para>
    /// This is an abstract class.
    /// </summary>
    internal abstract class ParsedStreamObject<TDictionary> : PdfObject, IStreamObject<TDictionary> where TDictionary : class, IStreamDictionary
    {
        private readonly TDictionary _dictionary;
        private readonly Stream _sourceData;

        /// <summary>
        /// Construct a <see cref="StreamObject{TDictionary}"/> from an existing source of data and a dictionary.<para></para>
        /// </summary>
        protected ParsedStreamObject(TDictionary dictionary, Stream sourceData)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _sourceData = sourceData ?? throw new ArgumentNullException(nameof(sourceData));
        }

        public TDictionary Dictionary => _dictionary;

        public async Task<Stream> GetDecompressedDataAsync()
        {
            // TODO: stream contents may be encrypted, decrypt.

            if (_dictionary.Filter is null)
            {
                return _sourceData;
            }

            var filterNames = _dictionary.Filter as ArrayObject ?? new[] { _dictionary.Filter };

            var allFilterParamsArray = _dictionary.DecodeParms as ArrayObject;

            var allFilterParams = allFilterParamsArray ??
                (_dictionary.DecodeParms is Dictionary singleFilterParamsDictionary ? new[] { singleFilterParamsDictionary } : (ArrayObject?)null);

            _sourceData.Position = 0;
            var content = await _sourceData.ReadToEndAsync();

            for (var i = 0; i < filterNames.Count(); i++)
            {
                var filterName = (Name)filterNames.ElementAt(i);
                var filterParams = (Dictionary?)allFilterParams?.ElementAtOrDefault(i);

                var filter = FilterFactory.Create(filterName, filterParams);

                content = filter.Decode(content);
            }

            return new MemoryStream(content);
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await _dictionary.WriteAsync(stream);

            await stream.WriteNewLineAsync();
            await new Keyword(Constants.StreamStart).WriteAsync(stream);

            await stream.WriteNewLineAsync();
            await _sourceData.CopyToAsync(stream);
            await stream.WriteNewLineAsync();

            await new Keyword(Constants.StreamEnd).WriteAsync(stream);
        }
    }
}
