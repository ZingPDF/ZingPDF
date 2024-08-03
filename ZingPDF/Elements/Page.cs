using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Filters;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;

namespace ZingPDF.Elements
{
    public class Page
    {
        private readonly IIndirectObjectDictionary _indirectObjectDictionary;

        internal Page(IndirectObject pageObject, IIndirectObjectDictionary indirectObjectManager)
        {
            IndirectObject = pageObject ?? throw new ArgumentNullException(nameof(pageObject));
            _indirectObjectDictionary = indirectObjectManager ?? throw new ArgumentNullException(nameof(indirectObjectManager));
        }

        private IndirectObjectManager IndirectObjects => (IndirectObjectManager)_indirectObjectDictionary;

        public IndirectObject IndirectObject { get; }
        public PageDictionary Dictionary => IndirectObject.Get<PageDictionary>();

        public void AddText(TextObject text)
        {
            EnsureEditable();
            ArgumentNullException.ThrowIfNull(text);

            Dictionary.AddContent([text], IndirectObjects);

            IndirectObjects.Update(IndirectObject);
        }

        public async Task AddImageAsync(Image image)
        {
            EnsureEditable();
            ArgumentNullException.ThrowIfNull(image);

            // TODO: Think about whether to implement inline images

            // TODO: configurable image size
            // TODO: configurable (or derived) image type and colorspace.
            // TODO: derive bit depth from image data
            // TODO: derive compression from image data

            var imageXObject = new ImageXObject(
                image.ImageData,
                100,
                100,
                ColorSpace.DeviceRGB,
                8,
                [FilterFactory.Create(Constants.Filters.DCT, null)],
                sourceDataIsCompressed: true);

            var imageXObjectIndirectObject = IndirectObjects.Add(imageXObject);

            // TODO: random short name, maybe each resource dictionary should manage this
            await Dictionary.AddXObjectResourceAsync("abcd", imageXObjectIndirectObject.Id.Reference, IndirectObjects);

            var imageContentStream = new ImageXObjectContentStreamObject("abcd", new Drawing.Coordinate(10, 10));

            Dictionary.AddContent([imageContentStream], IndirectObjects);
        }

        //// TODO: consider coordinate system enum.
        //// Should be consistent between this and text/image objects.
        //public void AddPath(Drawing.Path path)
        //{
        //    EnsureEditable();
        //    ArgumentNullException.ThrowIfNull(path);

        //    // TODO: PathObject
        //}

        //public void Rotate(Rotation rotation)
        //{
        //    EnsureEditable();

        //    ArgumentNullException.ThrowIfNull(rotation);

        //    // TODO: apply rotation to page
        //}

        private void EnsureEditable()
        {
            if (_indirectObjectDictionary is not IndirectObjectManager)
            {
                throw new InvalidOperationException("Page is immutable");
            }
        }
    }
}
