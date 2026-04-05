namespace ZingPDF.Graphics.Images
{
    internal enum CompressionType
    {
        None,
        DCT, // JPEG
        JPX, // JPEG 2000
        DEFLATE, // PNG
        RLE8, // BMP 8-Bit
        RLE4, // BMP 4-Bit
        LZW, // GIF, TIFF
        CCITTGroup3, // TIFF
        CCITTGroup4, // TIFF
        PackBits, // TIFF
    }

    internal static class CompressionTypeExtensions
    {
        public static string? ToPDFFilterName(this CompressionType compressionType)
        {
            return compressionType switch
            {
                CompressionType.None => null,
                CompressionType.DCT => Constants.Filters.DCT,
                CompressionType.JPX => Constants.Filters.JPX,
                CompressionType.DEFLATE => Constants.Filters.Flate,
                CompressionType.LZW => Constants.Filters.LZW,
                CompressionType.CCITTGroup3 or CompressionType.CCITTGroup4 => Constants.Filters.CCITT,
                CompressionType.RLE4 or CompressionType.RLE8 or CompressionType.PackBits => Constants.Filters.RunLength,
                _ => null,
            };
        }
    }
}
