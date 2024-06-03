using System.Drawing;
using System.IO.Compression;
using ZingPDF.ObjectModel.Filters.FilterUtils;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.ObjectModel.Filters
{
    internal class FlateDecodeFilter : IFilter
    {
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
            // During decompression, we don't need any info from the zlib header,
            // so we can skip it entirely, and just use DeflateStream.

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
            //byte[] predictedData = PngPredictor.Encode(data, predictor, colors, bitsPerComponent, columns);

            // PDF specifies zlib/deflate as the compression algorithm.
            // Zlib is just a metadata wrapper around deflate, so there's no need to use a zlib library.
            // DeflateStream is included in .NET, so we're using this to compress the data.
            // Then we're manually adding the zlib header and footer.

            using var memoryStream = new MemoryStream();

            // Add zlib header (CMF and FLG bytes)
            memoryStream.WriteByte(0x78); // CMF
            memoryStream.WriteByte(0xDA); // FLG

            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
            {
                deflateStream.Write(data, 0, data.Length);
            }

            // Add zlib footer (Adler-32 checksum)
            var adler32 = Adler32(data);
            memoryStream.WriteByte((byte)(adler32 >> 24));
            memoryStream.WriteByte((byte)(adler32 >> 16));
            memoryStream.WriteByte((byte)(adler32 >> 8));
            memoryStream.WriteByte((byte)adler32);

            return memoryStream.ToArray();
        }

        private static uint Adler32(byte[] data)
        {
            const uint Modulus = 65521;
            uint s1 = 1, s2 = 0;
            foreach (byte b in data)
            {
                s1 = (s1 + b) % Modulus;
                s2 = (s2 + s1) % Modulus;
            }
            return (s2 << 16) + s1;
        }
    }
}
