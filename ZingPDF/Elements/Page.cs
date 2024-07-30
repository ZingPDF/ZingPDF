using ZingPDF.Elements.Drawing;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects.IndirectObjects;

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

        public void AddText(TextBox text)
        {
            EnsureEditable();

            ArgumentNullException.ThrowIfNull(text);


        }

        public void AddImage(Image image)
        {
            EnsureEditable();

            ArgumentNullException.ThrowIfNull(image);


        }

        public void Draw(
            IEnumerable<Drawing.Path>? paths = null,
            IEnumerable<TextBox>? textBoxes = null,
            IEnumerable<Image>? images = null,
            CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
            )
        {
            EnsureEditable();

            if (paths == null && textBoxes == null && images == null) throw new ArgumentException("One of paths, textBoxes, or images is required");
            if ((paths != null && !paths.Any())
                || (textBoxes != null && !textBoxes.Any())
                || (images != null && !images.Any())
                ) throw new ArgumentException("Empty collection encountered");


        }

        public void Rotate(Rotation rotation)
        {
            EnsureEditable();

            ArgumentNullException.ThrowIfNull(rotation);


        }

        private void EnsureEditable()
        {
            if (_indirectObjectDictionary is IndirectObjectManager)
            {
                throw new InvalidOperationException("Page is immutable");
            }
        }
    }
}
