using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZingPDF.Elements;
using ZingPDF.Graphics.Images;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.OCR;

internal static class PageImageExtractor
{
    private const string DctDecode = "DCTDecode";
    private const string FlateDecode = "FlateDecode";
    private const string JpxDecode = "JPXDecode";
    private const string DeviceGray = "DeviceGray";
    private const string DeviceRgb = "DeviceRGB";

    public static async Task<OcrInputImage?> TryExtractBestCandidateAsync(Page page, IPdf pdf, CancellationToken cancellationToken)
    {
        var resourcesDictionary = await page.Dictionary.Resources.GetAsync();
        if (resourcesDictionary is null)
        {
            return null;
        }

        var xObjectDictionary = await ResourceDictionary.FromDictionary(resourcesDictionary).XObject.GetAsync();
        if (xObjectDictionary is null)
        {
            return null;
        }

        OcrInputImage? bestCandidate = null;
        long bestScore = -1;

        foreach (var entry in xObjectDictionary)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageStream = await TryResolveImageStreamAsync(entry.Value, pdf);
            if (imageStream is null)
            {
                continue;
            }

            var candidate = await TryCreateInputImageAsync(imageStream, cancellationToken);
            if (candidate is null)
            {
                continue;
            }

            var score = (long)candidate.Width * candidate.Height;
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private static async Task<StreamObject<ImageDictionary>?> TryResolveImageStreamAsync(IPdfObject value, IPdf pdf)
    {
        if (value is IndirectObjectReference reference)
        {
            return await pdf.Objects.GetAsync<StreamObject<ImageDictionary>>(reference);
        }

        return value as StreamObject<ImageDictionary>;
    }

    private static async Task<OcrInputImage?> TryCreateInputImageAsync(
        StreamObject<ImageDictionary> imageStream,
        CancellationToken cancellationToken)
    {
        var width = (int)await imageStream.Dictionary.Width.GetAsync();
        var height = (int)await imageStream.Dictionary.Height.GetAsync();
        var filterNames = await imageStream.Dictionary.Filter.GetAsync();
        var firstFilterName = filterNames?.OfType<Name>().Select(x => x.Value).FirstOrDefault();

        if (string.Equals(firstFilterName, DctDecode, StringComparison.Ordinal))
        {
            var data = await ReadAllBytesAsync(imageStream.Data, cancellationToken);

            return new OcrInputImage
            {
                PageNumber = 0,
                Width = width,
                Height = height,
                MimeType = "image/jpeg",
                Data = data
            };
        }

        if (string.Equals(firstFilterName, JpxDecode, StringComparison.Ordinal))
        {
            var data = await ReadAllBytesAsync(imageStream.Data, cancellationToken);

            return new OcrInputImage
            {
                PageNumber = 0,
                Width = width,
                Height = height,
                MimeType = "image/jp2",
                Data = data
            };
        }

        if (firstFilterName is null || string.Equals(firstFilterName, FlateDecode, StringComparison.Ordinal))
        {
            var colorSpaceName = (await imageStream.Dictionary.ColorSpace.GetAsync() as Name)?.Value;
            var bitsPerComponent = await imageStream.Dictionary.BitsPerComponent.GetAsync();
            var pngBytes = await TryEncodeRawImageAsPngAsync(
                imageStream,
                colorSpaceName,
                bitsPerComponent is null ? null : (int)bitsPerComponent,
                width,
                height,
                cancellationToken);

            if (pngBytes is not null)
            {
                return new OcrInputImage
                {
                    PageNumber = 0,
                    Width = width,
                    Height = height,
                    MimeType = "image/png",
                    Data = pngBytes
                };
            }
        }

        return null;
    }

    private static async Task<byte[]?> TryEncodeRawImageAsPngAsync(
        StreamObject<ImageDictionary> imageStream,
        string? colorSpaceName,
        int? bitsPerComponent,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        if (bitsPerComponent != 8)
        {
            return null;
        }

        await using var decoded = await imageStream.GetDecompressedDataAsync();
        var rawBytes = await ReadAllBytesAsync(decoded, cancellationToken);

        if (string.Equals(colorSpaceName, DeviceGray, StringComparison.Ordinal))
        {
            if (rawBytes.Length != width * height)
            {
                return null;
            }

            using var image = SixLabors.ImageSharp.Image.LoadPixelData<L8>(rawBytes, width, height);
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new PngEncoder(), cancellationToken);
            return output.ToArray();
        }

        if (string.Equals(colorSpaceName, DeviceRgb, StringComparison.Ordinal))
        {
            if (rawBytes.Length != width * height * 3)
            {
                return null;
            }

            using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgb24>(rawBytes, width, height);
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new PngEncoder(), cancellationToken);
            return output.ToArray();
        }

        return null;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var output = new MemoryStream();
        await stream.CopyToAsync(output, cancellationToken);
        return output.ToArray();
    }
}
