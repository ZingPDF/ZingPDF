using ZingPDF.Syntax.Filters;
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
        public ImageXObject(Stream image, IEnumerable<IFilter>? filters = null) : base(filters)
        {
        }

        protected override Task<Stream> GetSourceDataAsync(ImageDictionary dictionary)
        {
            throw new NotImplementedException();
        }

        protected override Task<ImageDictionary> GetSpecialisedDictionaryAsync()
        {
            throw new NotImplementedException();
        }
    }
}
