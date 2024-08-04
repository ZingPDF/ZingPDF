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
}
