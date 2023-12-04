using System.IO.Compression;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    internal class FlateDecodeFilter : IFilter
    {
        private const int _defaultPredictor = 1;
        private const int _defaultColors = 1;
        private const int _defaultBitsPerComponent = 8;
        private const int _defaultColumns = 1;

        public FlateDecodeFilter(Dictionary? filterParams)
        {
            Params = filterParams ?? new();
        }

        public Name Name => Constants.Filters.Flate;
        public Dictionary Params { get; }

        public byte[] Decode(byte[] data)
        {
            using var output = new MemoryStream();
            using var input = new MemoryStream(data[2..]);
            using var decoder = new DeflateStream(input, CompressionMode.Decompress);

            decoder.CopyTo(output);

            return PngPredictor.Decode(
                output.ToArray(),
                Params.Get<Integer>("Predictor") ?? _defaultPredictor,
                Params.Get<Integer>("Columns") ?? _defaultColumns,
                Params.Get<Integer>("Colors") ?? _defaultColors,
                Params.Get<Integer>("BitsPerComponent") ?? _defaultBitsPerComponent
                );
        }

        public byte[] Encode(byte[] data)
        {
            using var output = new MemoryStream();
            using var input = new MemoryStream(data);
            using var encoder = new DeflateStream(output, CompressionMode.Compress);

            input.CopyTo(encoder);
            encoder.Flush();

            return output.ToArray();
        }
    }
}
