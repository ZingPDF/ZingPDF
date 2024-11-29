using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
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
internal class ImageXObjectFactory : IStreamObjectFactory<ImageDictionary>
{
    private readonly Stream _image;
    private readonly Integer _width;
    private readonly Integer _height;
    private readonly ColorSpace _colorSpace;
    private readonly Integer _bitDepth;
    private readonly IEnumerable<IFilter>? _filters;
    private readonly bool _sourceDataIsCompressed;

    public ImageXObjectFactory(
        Stream image,
        Integer width,
        Integer height,
        ColorSpace colorSpace,
        Integer bitDepth,
        IEnumerable<IFilter>? filters,
        bool sourceDataIsCompressed
        )
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        _width = width ?? throw new ArgumentNullException(nameof(width));
        _height = height ?? throw new ArgumentNullException(nameof(height));
        _colorSpace = colorSpace;
        _bitDepth = bitDepth ?? throw new ArgumentNullException(nameof(bitDepth));
        _filters = filters;
        _sourceDataIsCompressed = sourceDataIsCompressed;
    }

    public StreamObject<ImageDictionary> Create()
    {
        return new StreamObject<ImageDictionary>(
            new StreamData(_image, _sourceDataIsCompressed, _filters),
            new ImageDictionary(_width, _height, (Name)_colorSpace.ToString(), _bitDepth)
            );
    }
}
