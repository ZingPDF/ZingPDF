
namespace ZingPDF.Graphics.Images
{
    internal class ImageMetadata
    {
        public ImageMetadata(ImageType type, int bitDepth, ColorSpace colorSpace, CompressionType compressionType)
        {
            Type = type;
            BitDepth = bitDepth;
            ColorSpace = colorSpace;
            CompressionType = compressionType;
        }

        public ImageType Type { get; }
        public int BitDepth { get; }
        public ColorSpace ColorSpace { get; }
        public CompressionType CompressionType { get; }
    }
}
