
namespace ZingPDF.Graphics.Images
{
    internal class ImageMetadata
    {
        public ImageMetadata(
            ImageType type,
            int bitDepth,
            ColorSpace colorSpace,
            CompressionType compressionType,
            int width,
            int height
            )
        {
            Type = type;
            BitDepth = bitDepth;
            ColorSpace = colorSpace;
            CompressionType = compressionType;
            Width = width;
            Height = height;
        }

        public ImageType Type { get; }
        public int BitDepth { get; }
        public ColorSpace ColorSpace { get; }
        public CompressionType CompressionType { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
