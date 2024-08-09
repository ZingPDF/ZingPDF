using ZingPDF.Extensions;
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
            _indirectObjectDictionary.EnsureEditable();
            ArgumentNullException.ThrowIfNull(text);

            Dictionary.AddContent([text], IndirectObjects);

            IndirectObjects.Update(IndirectObject);
        }

        public async Task AddImageAsync(Image image)
        {
            _indirectObjectDictionary.EnsureEditable();
            ArgumentNullException.ThrowIfNull(image);

            // TODO: Think about whether to implement inline images

            var imageMetadata = await new ImageSharpMetadataRetriever().GetAsync(image.ImageData);

            List<IFilter>? filters = null;
            var filter = imageMetadata.CompressionType.ToPDFFilterName();

            if (filter != null)
            {
                // TODO: derive required params, possibly only for CCITT compression.
                filters = [FilterFactory.Create(filter, null)];
            }

            var imageXObject = new ImageXObject(
                image.ImageData,
                imageMetadata.Width,
                imageMetadata.Height,
                imageMetadata.ColorSpace,
                imageMetadata.BitDepth,
                filters,
                sourceDataIsCompressed: true
                );

            var imageXObjectIndirectObject = IndirectObjects.Add(imageXObject);

            var resourceName = UniqueStringGenerator.Generate();
            await Dictionary.AddXObjectResourceAsync(resourceName, imageXObjectIndirectObject.Id.Reference, IndirectObjects);

            var imageRect = image.MaxBounds;
            if (image.PreserveAspectRatio)
            {
                var (newWidth, newHeight) = ScaleToFit(imageMetadata.Width, imageMetadata.Height, image.MaxBounds.Width, image.MaxBounds.Height);

                imageRect = new Syntax.CommonDataStructures.Rectangle(
                    image.MaxBounds.LowerLeft,
                    new Drawing.Coordinate(
                        image.MaxBounds.LowerLeft.X + newWidth,
                        image.MaxBounds.LowerLeft.Y + newHeight
                        )
                    );
            }
            
            var imageContentStream = new ImageXObjectContentStreamObject(resourceName, imageRect);

            Dictionary.AddContent([imageContentStream], IndirectObjects);

            IndirectObjects.Update(IndirectObject);
        }

        //// TODO: consider coordinate system enum.
        //// Should be consistent between this and text/image objects.
        //public void AddPath(Drawing.Path path)
        //{
        //    EnsureEditable();
        //    ArgumentNullException.ThrowIfNull(path);

        //    // TODO: PathObject
        //}

        public void Rotate(Rotation rotation)
        {
            // TODO: Ensure contents don't need some sort of transform to match

            _indirectObjectDictionary.EnsureEditable();
            ArgumentNullException.ThrowIfNull(rotation);

            // The page may already be rotated, or inherit a value for rotation.
            // In practice, it is likely desired to rotate by a further n degrees.
            Dictionary.SetRotation((Dictionary.Rotate ?? 0) + rotation);

            IndirectObjects.Update(IndirectObject);
        }

        // TODO: move to testable class
        private static (int newWidth, int newHeight) ScaleToFit(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // Calculate aspect ratios
            float aspectRatioOriginal = originalWidth / (float)originalHeight;
            float aspectRatioMax = maxWidth / maxHeight;

            // Determine scaling factor based on which dimension is more restrictive
            float newWidth, newHeight;
            if (aspectRatioOriginal > aspectRatioMax)
            {
                // Scale based on maxWidth
                newWidth = maxWidth;
                newHeight = maxWidth / aspectRatioOriginal;
            }
            else
            {
                // Scale based on maxHeight
                newWidth = maxHeight * aspectRatioOriginal;
                newHeight = maxHeight;
            }

            return ((int)newWidth, (int)newHeight);
        }
    }
}
