using System.IO.Compression;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    internal class FlateDecodeFilter : IFilter
    {
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

            return output.ToArray();
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
