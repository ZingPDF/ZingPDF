using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics.Images;

/// <summary>
/// <para>ISO 32000-2:2020 8.9 - Images</para>
/// 
/// <para>
/// An image XObject (described in 8.9.5, "Image dictionaries") is a stream object whose dictionary specifies 
/// attributes of the image and whose data contains the image samples. Like all external objects, it is 
/// painted on the page by invoking the Do operator in a content stream (see 8.8, "External objects"). 
/// Image XObjects have other uses as well, such as for alternate images (see 8.9.5.4, "Alternate images"), 
/// image masks (8.9.6, "Masked images"), and thumbnail images (12.3.4, "Thumbnail images").
/// </para>
/// </summary>
internal class ImageXObjectFactory(
    Stream image,
    Number width,
    Number height,
    ColorSpace colorSpace,
    Number bitDepth,
    ShorthandArrayObject? filter,
    ShorthandArrayObject? decodeParms,
    Dictionary? f,
    ShorthandArrayObject? fFilter,
    ShorthandArrayObject? fDecodeParms,
    Number? dL
    )
    : IStreamObjectFactory<ImageDictionary>
{
    private readonly Stream _image = image ?? throw new ArgumentNullException(nameof(image));
    private readonly Number _width = width ?? throw new ArgumentNullException(nameof(width));
    private readonly Number _height = height ?? throw new ArgumentNullException(nameof(height));
    private readonly Number _bitDepth = bitDepth ?? throw new ArgumentNullException(nameof(bitDepth));

    public StreamObject<ImageDictionary> Create(IPdfEditor pdfEditor)
    {
        return new StreamObject<ImageDictionary>(
            _image,
            new ImageDictionary(
                _width,
                _height,
                (Name)colorSpace.ToString(),
                _bitDepth,
                _image.Length,
                filter,
                decodeParms,
                f,
                fFilter,
                fDecodeParms,
                dL,
                pdfEditor
                )
            );
    }
}
