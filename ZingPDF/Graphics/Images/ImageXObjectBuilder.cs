using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Syntax.Filters;

namespace ZingPDF.Graphics.Images;

internal static class ImageXObjectBuilder
{
    public static async Task<PreparedImageXObject> CreateAsync(Stream imageData)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        try
        {
            var metadata = await new ImageSharpMetadataRetriever().GetAsync(imageData);

            if (metadata.CompressionType is CompressionType.DCT or CompressionType.JPX)
            {
                return await CreateDirectImageAsync(imageData, metadata);
            }

            return await CreateRasterImageAsync(imageData);
        }
        finally
        {
            if (imageData.CanSeek)
            {
                imageData.Position = 0;
            }
        }
    }

    private static async Task<PreparedImageXObject> CreateDirectImageAsync(Stream imageData, ImageMetadata metadata)
    {
        imageData.Position = 0;

        var copiedImageData = new MemoryStream();
        await imageData.CopyToAsync(copiedImageData);
        copiedImageData.Position = 0;

        return new PreparedImageXObject(
            copiedImageData,
            metadata.Width,
            metadata.Height,
            metadata.ColorSpace.ToString(),
            metadata.BitDepth,
            metadata.CompressionType.ToPDFFilterName());
    }

    private static async Task<PreparedImageXObject> CreateRasterImageAsync(Stream imageData)
    {
        imageData.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(imageData);

        var rgbBytes = new byte[image.Width * image.Height * 3];
        var alphaBytes = new byte[image.Width * image.Height];

        var rgbOffset = 0;
        var alphaOffset = 0;
        var hasTransparency = false;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];

                    rgbBytes[rgbOffset++] = pixel.R;
                    rgbBytes[rgbOffset++] = pixel.G;
                    rgbBytes[rgbOffset++] = pixel.B;

                    alphaBytes[alphaOffset++] = pixel.A;
                    hasTransparency |= pixel.A < byte.MaxValue;
                }
            }
        });

        var encodedRgbData = EncodeFlate(rgbBytes);
        PreparedImageXObject? softMask = null;

        if (hasTransparency)
        {
            softMask = new PreparedImageXObject(
                EncodeFlate(alphaBytes),
                image.Width,
                image.Height,
                ColorSpace.DeviceGray.ToString(),
                8,
                Constants.Filters.Flate);
        }

        return new PreparedImageXObject(
            encodedRgbData,
            image.Width,
            image.Height,
            ColorSpace.DeviceRGB.ToString(),
            8,
            Constants.Filters.Flate,
            softMask);
    }

    private static MemoryStream EncodeFlate(byte[] bytes)
    {
        using var rawData = new MemoryStream(bytes, writable: false);
        return new FlateDecodeFilter(null).Encode(rawData);
    }
}
