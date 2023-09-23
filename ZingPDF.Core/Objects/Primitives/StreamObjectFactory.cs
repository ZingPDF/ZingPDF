using System.Linq;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Filters;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class StreamObjectFactory
    {
        private readonly IndirectObjectCollection _indirectObjectCollection;

        public StreamObjectFactory(IndirectObjectCollection indirectObjectCollection)
        {
            _indirectObjectCollection = indirectObjectCollection;
        }

        public IndirectObject Create(byte[] data, IEnumerable<IFilter> filters)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (filters is null) throw new ArgumentNullException(nameof(filters));

            var decodedDataLength = data.Length;

            string content = "";
            foreach (var filter in filters)
            {
                content = filter.Encode(data);
                data = Encoding.ASCII.GetBytes(content);
            }

            var streamDictionary = new Dictionary<Name, PdfObject>()
            {
                { "Length", new Integer(data.Length - filters.Last().EndOfDataMarker.Length) },
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

            return _indirectObjectCollection.Add(new Dictionary(streamDictionary), new StreamObject(content));
        }

        /// <summary>
        /// PDF 32000-1:2008 7.3.8
        /// </summary>
        private class StreamObject : PdfObject
        {
            private readonly string _data;

            public StreamObject(string data)
            {
                if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException($"'{nameof(data)}' cannot be null or whitespace.", nameof(data));

                _data = data;
            }

            public override async Task WriteOutputAsync(Stream stream)
            {
                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamStart);
                await stream.WriteNewLineAsync();

                await stream.WriteTextAsync(_data);

                await stream.WriteNewLineAsync();
                await stream.WriteTextAsync(Constants.StreamEnd);
            }
        }
    }
}
