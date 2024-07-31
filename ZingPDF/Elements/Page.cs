using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure.PageTree;
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

        public IndirectObject IndirectObject { get; }
        public PageDictionary Dictionary => IndirectObject.Get<PageDictionary>();

        public void AddText(TextObject text)
        {
            EnsureEditable();
            ArgumentNullException.ThrowIfNull(text);

            var indirectObjectManager = (IndirectObjectManager)_indirectObjectDictionary;

            Dictionary.AddContent([text], indirectObjectManager);

            indirectObjectManager.Update(IndirectObject);
        }

        //public void AddImage(ImageObject image)
        //{
        //    EnsureEditable();
        //    ArgumentNullException.ThrowIfNull(image);

        //    // TODO: ImageObject
        //    //Dictionary.AddContent(image, (IndirectObjectManager)_indirectObjectDictionary);
        //}

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
