using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Syntax.Filters
{
    internal class DCTDecodeFilter(Dictionary? filterParams) : IFilter
    {
        public Name Name => Constants.Filters.DCT;

        public Dictionary? Params { get; } = filterParams;

        public byte[] Decode(byte[] data)
        {
            using var inputStream = new MemoryStream(data);
            using var image = Image.Load<Rgba32>(inputStream);
            using var outputStream = new MemoryStream();

            image.Save(outputStream, new BmpEncoder());

            return outputStream.ToArray();
        }

        public byte[] Encode(byte[] data)
        {
            using var inputStream = new MemoryStream(data);
            using var image = Image.Load<Rgba32>(inputStream);
            using var outputStream = new MemoryStream();

            image.Save(outputStream, new JpegEncoder());

            return outputStream.ToArray();
        }
    }
}
