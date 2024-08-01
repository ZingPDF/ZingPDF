using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Streams;

namespace ZingPDF.Graphics.Images
{
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
    internal class ImageXObject : StreamObject<ImageDictionary>
    {
        private readonly Stream _image;
        private readonly ImageType _imageType;
        private readonly Integer _width;
        private readonly Integer _height;
        private readonly ColorSpace _colorSpace;

        private readonly ImageDictionary _imageDictionary;
        private readonly List<Name> _filters;

        public ImageXObject(Stream image, ImageType imageType, Integer width, Integer height, ColorSpace colorSpace)
            : base(null)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _imageType = imageType;
            _width = width ?? throw new ArgumentNullException(nameof(width));
            _height = height ?? throw new ArgumentNullException(nameof(height));
            _colorSpace = colorSpace;

            // TODO: derive bit depth from image data
            // TODO: derive compression from image data

            switch (_imageType)
            {
                case ImageType.Jpeg:
                    _imageDictionary = new ImageDictionary(_width, _height, (Name)_colorSpace.ToString(), 8);
                    _filters = [Constants.Filters.DCT];
                    break;
                case ImageType.Jpeg2000:
                    _imageDictionary = new ImageDictionary(_width, _height, (Name)_colorSpace.ToString(), 8);
                    _filters = [Constants.Filters.JPX];
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected override Task<Stream> GetSourceDataAsync(ImageDictionary dictionary)
        {
            return Task.FromResult(_image);
        }

        protected override Task<ImageDictionary> GetSpecialisedDictionaryAsync()
        {
            return Task.FromResult(_imageDictionary);
        }
    }
}
