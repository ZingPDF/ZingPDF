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

        public MemoryStream Decode(Stream data)
        {
            using var image = Image.Load<Rgba32>(data);
            using var outputStream = new MemoryStream();

            image.Save(outputStream, new BmpEncoder());

            return outputStream;
        }

        public MemoryStream Encode(Stream data)
        {
            using var image = Image.Load<Rgba32>(data);
            using var outputStream = new MemoryStream();

            image.Save(outputStream, new JpegEncoder());

            return outputStream;
        }
    }
}
