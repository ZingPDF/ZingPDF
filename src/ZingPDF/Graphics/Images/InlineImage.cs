using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;

namespace ZingPDF.Graphics.Images
{
    /// <summary>
    /// <para>ISO 32000-2:2020 8.9 - Images</para>
    /// <para>
    /// An inline image is a small image that is completely defined — both attributes and data — directly 
    /// inline within a content stream. The kinds of images that can be represented in this way are limited; 
    /// see 8.9.7, "Inline images" for details.
    /// </para>
    /// </summary>
    internal class InlineImage : ContentStream
    {
        public InlineImage(ObjectContext context)
            : base(context)
        {
        }

        protected override Task WriteOutputAsync(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
