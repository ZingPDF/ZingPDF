using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Filters;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8 - Stream objects
    /// </summary>
    internal class StreamObject : PdfObject
    {
        private readonly Stream _source;
        private readonly long _from;
        private readonly long _to;
        private readonly Dictionary _streamDictionary;

        private StreamObject(Stream source, long from, long to, Dictionary streamDictionary)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _from = from;
            _to = to;
            _streamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
        }

        private StreamObject(byte[] unencodedData, IEnumerable<IFilter> filters)
        {
            filters ??= Enumerable.Empty<IFilter>();

            var unencodedLength = unencodedData.Length;

            byte[] encodedData = unencodedData;
            foreach (var filter in filters)
            {
                encodedData = filter.Encode(encodedData);
            }

            _source = new MemoryStream(encodedData);
            _from = 0;
            _to = _source.Length;

            _streamDictionary = BuildStreamDictionary(unencodedLength, filters);
        }

        /// <summary>
        /// Used when building a PDF to create a StreamObject from byte data.
        /// </summary>
        public static StreamObject FromUnencodedData(byte[] unencodedData, IEnumerable<IFilter> filters)
            => new(unencodedData, filters);

        /// <summary>
        /// Used when parsing a PDF from a file or other type of system stream.
        /// </summary>
        public static StreamObject FromEncodedStream(Stream stream, long from, long to, Dictionary streamDictionary)
            => new(stream, from, to, streamDictionary);

        public async Task<byte[]> DecodeAsync()
        {
            // TODO: stream contents may be encrypted, decrypt.

            var filterNamesEntry = _streamDictionary["Filter"];
            var filterNames = filterNamesEntry as Array ?? new[] { filterNamesEntry };

            var allFilterParamsArray = _streamDictionary.Get<Array>("DecodeParms");
            var singleFilterParamsDictionary = _streamDictionary.Get<Dictionary>("DecodeParms");

            var allFilterParams = allFilterParamsArray ??
                (singleFilterParamsDictionary != null ? new[] { singleFilterParamsDictionary } : (Array?)null);

            var relevantRange = await _source.RangeAsync(_from, _to);

            var content = await relevantRange.ReadToEndAsync();

            for(var i = 0; i < filterNames.Count(); i++)
            {
                var filterName = (Name)filterNames.ElementAt(i);
                var filterParams = (Dictionary?)allFilterParams?.ElementAtOrDefault(i);

                var filter = FilterFactory.Create(filterName, filterParams);

                content = filter.Decode(content);
            }

            return content;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await _streamDictionary.WriteAsync(stream);

            await new Keyword(Constants.StreamStart).WriteAsync(stream);
            await new StreamData(_source, _from, _to).WriteAsync(stream);
            await new Keyword(Constants.StreamEnd).WriteAsync(stream);
        }

        private Dictionary BuildStreamDictionary(long unencodedLength, IEnumerable<IFilter> filters)
        {
            var streamDictionary = new Dictionary<Name, PdfObject>()
                {
                    { "Length", new Integer(_to - _from) },
                    { "DL", new Integer(unencodedLength) },
                };

            if (filters.Any())
            {
                streamDictionary.Add("Filter", new Array(filters.Select(f => f.Name).ToArray()));

                if (filters.Any(f => f.Params != null))// && f.Params.Modified))
                {
                    if (filters.Count() == 1)
                    {
                        streamDictionary.Add("DecodeParms", filters.First().Params!);
                    }
                    else
                    {
                        streamDictionary.Add("DecodeParms", new Array(filters.Select(f =>
                        {
                            if (f.Params != null)// && f.Params.Modified)
                            {
                                return (PdfObject)f.Params;
                            }
                            else
                            {
                                return new Null();
                            }
                        }).ToArray()));
                    }
                }
            }

            // TODO: add support for external file (F, FFilter, FDecodeParms
            return streamDictionary;
        }

        private class StreamData : PdfObject
        {
            private readonly Stream _source;
            private readonly long _from;
            private readonly long _to;

            public StreamData(Stream source, long from, long to)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _from = from;
                _to = to;
            }

            protected override async Task WriteOutputAsync(Stream stream)
            {
                var sourceRange = await _source.RangeAsync(_from, _to);

                await sourceRange.CopyToAsync(stream);
            }
        }
    }
}
