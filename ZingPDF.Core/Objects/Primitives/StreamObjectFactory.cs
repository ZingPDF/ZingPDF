using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Filters;

namespace ZingPdf.Core.Objects.Primitives
{
    /// <summary>
    /// ISO 32000-2:2020 7.3.8 - Stream objects
    /// </summary>
    internal class StreamObjectFactory
    {
        private readonly IndirectObjectManager _indirectObjectCollection;

        public StreamObjectFactory(IndirectObjectManager indirectObjectCollection)
        {
            _indirectObjectCollection = indirectObjectCollection;
        }

        public IndirectObject Create(byte[] data, IEnumerable<IFilter> filters)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (filters is null) throw new ArgumentNullException(nameof(filters));

            var decodedDataLength = data.Length;

            foreach (var filter in filters)
            {
                data = filter.Encode(data);
            }

            var streamDictionary = new Dictionary<Name, PdfObject>()
            {
                { "Length", new Integer(data.Length) },
                { "DL", new Integer(decodedDataLength) }
            };

            if (filters.Any())
            {
                streamDictionary.Add("Filter", new Array(filters.Select(f => f.Name).ToArray()));

                if (filters.Any(f => f.Params != null && f.Params.Modified))
                {
                    if (filters.Count() == 1)
                    {
                        streamDictionary.Add("DecodeParms", filters.First().Params!);
                    }
                    else
                    {
                        streamDictionary.Add("DecodeParms", new Array(filters.Select(f =>
                        {
                            if (f.Params != null && f.Params.Modified)
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

            return _indirectObjectCollection.Add(new Dictionary(streamDictionary), new StreamObject(data));
        }

        private class StreamObject : PdfObject
        {
            private readonly byte[] _data;

            public StreamObject(byte[] data)
            {
                _data = data ?? throw new ArgumentNullException(nameof(data));
            }

            protected override async Task WriteOutputAsync(Stream stream)
            {
                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamStart);
                await stream.WriteNewLineAsync();

                await stream.WriteAsync(_data);

                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamEnd);
            }
        }
    }
}
