using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace ZingPDF.Graphics.Images
{
    internal class ImageSharpMetadataRetriever : IImageMetadataRetriever
    {
        public async Task<ImageMetadata> GetAsync(Stream image)
        {
            image.Position = 0;

            var imageType = GetImageType(image);
            var compressionType = await GetImageCompressionTypeAsync(imageType, image);

            var imageSharpImage = await Image.LoadAsync(image);
            var bitsPerComponent = GetBitsPerComponent(imageSharpImage);
            var colorSpace = GetColorSpace(imageSharpImage);

            return new ImageMetadata(imageType, bitsPerComponent, colorSpace, compressionType);
        }

        private static ImageType GetImageType(Stream image)
        {
            ImageType? imageType = null;
            image.Position = 0;

            byte[] headerBytes = new byte[8];

            image.Read(headerBytes, 0, headerBytes.Length);

            // JPEG (DCT or JPX)
            if (headerBytes[0] == 0xFF && headerBytes[1] == 0xD8)
            {
                imageType = ImageType.Jpeg;
            }

            // PNG
            if (headerBytes[0] == 0x89 && headerBytes[1] == 0x50 &&
                headerBytes[2] == 0x4E && headerBytes[3] == 0x47)
            {
                imageType = ImageType.Png;
            }

            // GIF
            if (headerBytes[0] == 0x47 && headerBytes[1] == 0x49 &&
                headerBytes[2] == 0x46)
            {
                imageType = ImageType.Gif;
            }

            // BMP
            if (headerBytes[0] == 0x42 && headerBytes[1] == 0x4D)
            {
                imageType = ImageType.Bmp;
            }

            // TIFF
            if ((headerBytes[0] == 0x49 && headerBytes[1] == 0x49 && headerBytes[2] == 0x2A && headerBytes[3] == 0x00) ||
                (headerBytes[0] == 0x4D && headerBytes[1] == 0x4D && headerBytes[2] == 0x00 && headerBytes[3] == 0x2A))
            {
                imageType = ImageType.Tiff;
            }

            image.Position = 0;

            return imageType ?? throw new NotSupportedException("Unknown image type encountered");
        }

        private static async Task<CompressionType> GetImageCompressionTypeAsync(ImageType imageType, Stream image)
        {
            return imageType switch
            {
                ImageType.Jpeg => await GetJpgCompressionTypeAsync(image),
                ImageType.Png => CompressionType.DEFLATE,
                ImageType.Gif => CompressionType.LZW,
                ImageType.Bmp => GetBmpCompressionType(image),
                ImageType.Tiff => GetTiffCompressionType(image),
                _ => throw new NotSupportedException("Unknown compression type encountered"),
            };
        }

        private static async Task<CompressionType> GetJpgCompressionTypeAsync(Stream image)
        {
            image.Position = 0;
            byte[] headerBytes = new byte[12];

            await image.ReadAsync(headerBytes);

            image.Position = 0;

            // Check for JPEG 2000 signature (JP2)
            if(headerBytes[0] == 0x00 && headerBytes[1] == 0x00
                && headerBytes[2] == 0x00 && headerBytes[3] == 0x0C
                && headerBytes[4] == 0x6A && headerBytes[5] == 0x50
                && headerBytes[6] == 0x20 && headerBytes[7] == 0x20)
            {
                return CompressionType.JPX;
            }
            else
            {
                return CompressionType.DCT;
            }
        }

        private static CompressionType GetBmpCompressionType(Stream image)
        {
            image.Position = 0;

            using var reader = new BinaryReader(image, Encoding.Default, leaveOpen: true);

            reader.BaseStream.Seek(14, SeekOrigin.Begin); // Skip to BitmapInfoHeader
            uint compression = reader.ReadUInt32(); // Read compression type

            return compression switch
            {
                0 => CompressionType.None,
                1 => CompressionType.RLE8,
                2 => CompressionType.RLE4,
                _ => throw new NotSupportedException("Unknown BMP compression encountered")
            };
        }

        private static CompressionType GetTiffCompressionType(Stream image)
        {
            using var reader = new BinaryReader(image, Encoding.Default, leaveOpen: true);

            reader.BaseStream.Seek(4, SeekOrigin.Begin); // Skip to IFD offset
            uint offset = reader.ReadUInt32(); // Read IFD offset

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            ushort numberOfTags = reader.ReadUInt16();

            for (int i = 0; i < numberOfTags; i++)
            {
                ushort tagType = reader.ReadUInt16();
                reader.BaseStream.Seek(6, SeekOrigin.Current); // Skip to value

                if (tagType == 259) // Compression tag
                {
                    ushort compression = reader.ReadUInt16();
                    return compression switch
                    {
                        1 => CompressionType.None,
                        2 => CompressionType.CCITTGroup3,
                        3 => CompressionType.CCITTGroup4,
                        4 => CompressionType.LZW,
                        5 => CompressionType.DCT,
                        6 => CompressionType.DCT,
                        7 => CompressionType.PackBits,
                        8 => CompressionType.DEFLATE,
                        34712 => CompressionType.JPX,
                        _ => throw new NotSupportedException("Unsupported TIFF compression type encountered")
                    };
                }

                reader.BaseStream.Seek(4, SeekOrigin.Current); // Skip to next tag
            }

            throw new NotSupportedException("Unsupported TIFF compression type encountered");
        }

        private static int GetBitsPerComponent(Image image)
        {
            int bitsPerPixel = image.PixelType.BitsPerPixel;

            int channels = image switch
            {
                Image<L8> => 1,
                Image<Rgb24> => 3,
                Image<Rgba32> => 4,
                Image<Bgr24> => 3,
                Image<Bgra32> => 4,
                _ => throw new NotSupportedException("Unsupported pixel format.")
            };

            return bitsPerPixel / channels;
        }

        private static ColorSpace GetColorSpace(Image image)
        {
            // TODO: think about support for CMYK images. ImageSharp does not support them.

            return image switch
            {
                Image<L8> => ColorSpace.DeviceGray,
                Image<Rgb24> => ColorSpace.DeviceRGB,
                Image<Rgba32> => ColorSpace.DeviceRGB,
                Image<Bgr24> => ColorSpace.DeviceRGB,
                Image<Bgra32> => ColorSpace.DeviceRGB,
                _ => throw new NotSupportedException("Unsupported pixel format.")
            };
        }
    }
}
