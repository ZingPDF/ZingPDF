using System.IO.Compression;
using ZingPDF.Objects.Filters.FilterUtils;
using ZingPDF.Objects.Primitives;

namespace ZingPDF.Objects.Filters
{
    internal class FlateDecodeFilter : IFilter
    {
        private const byte _deflate32KbWindow = 120;
        private const byte _checksumBits = 1;

        private const int _defaultPredictor = 1;
        private const int _defaultColors = 1;
        private const int _defaultBitsPerComponent = 8;
        private const int _defaultColumns = 1;

        public FlateDecodeFilter(Dictionary? filterParams)
        {
            Params = filterParams ?? [];
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
            // TODO: Do we need to consider implementing PNG predictor for compression?

            using var output = new MemoryStream();
            using var input = new MemoryStream(data);
            using var encoder = new DeflateStream(output, CompressionMode.Compress);

            // Write ZLib header.
            output.WriteByte(_deflate32KbWindow);
            output.WriteByte(_checksumBits);

            input.CopyTo(encoder);
            encoder.Flush();

            // Write checksum
            var checksum = Adler32Checksum.Calculate(data);

            output.WriteByte((byte)(checksum >> 24));
            output.WriteByte((byte)(checksum >> 16));
            output.WriteByte((byte)(checksum >> 8));
            output.WriteByte((byte)(checksum >> 0));

            return output.ToArray();
        }
    }
}
