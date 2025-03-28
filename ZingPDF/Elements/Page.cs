using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text;

namespace ZingPDF.Elements
{
    public class Page
    {
        private readonly IPdfEditor _pdfEditor;

        internal Page(IndirectObject pageObject, IPdfEditor pdfEditor)
        {
            ArgumentNullException.ThrowIfNull(pageObject, nameof(pageObject));
            ArgumentNullException.ThrowIfNull(pdfEditor, nameof(pdfEditor));

            IndirectObject = pageObject;
            _pdfEditor = pdfEditor;
        }

        public IndirectObject IndirectObject { get; }
        public PageDictionary Dictionary => (PageDictionary)IndirectObject.Object;

        public async Task AddTextAsync(TextObject text)
        {
            ArgumentNullException.ThrowIfNull(text);

            await Dictionary.AddContentAsync([text], _pdfEditor);

            _pdfEditor.Update(IndirectObject);
        }

        public async Task AddImageAsync(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            // TODO: Think about whether to implement inline images

            var imageMetadata = await new ImageSharpMetadataRetriever().GetAsync(image.ImageData);

            //List<IFilter> filters = [];
            var filter = imageMetadata.CompressionType.ToPDFFilterName();

            ShorthandArrayObject? filterAry = filter != null ? new ShorthandArrayObject([(Name)filter]) : null;

            //if (filter != null)
            //{
            //    // TODO: derive required params, possibly only for CCITT compression.
            //    filters = [FilterFactory.Create(filter, null)];
            //}

            var imageXObject = new ImageXObjectFactory(
                image.ImageData,
                imageMetadata.Width,
                imageMetadata.Height,
                imageMetadata.ColorSpace,
                imageMetadata.BitDepth,
                filterAry,
                decodeParms: null,
                f: null,
                fFilter: null,
                fDecodeParms: null,
                dL: null
                //sourceDataIsCompressed: filters.Count > 0 // TODO: This might be wrong. Test image adding
                )
                .Create(_pdfEditor);

            var imageXObjectIndirectObject = _pdfEditor.Add(imageXObject);

            var resourceName = UniqueStringGenerator.Generate();
            await Dictionary.AddXObjectResourceAsync(resourceName, imageXObjectIndirectObject.Id.Reference, _pdfEditor);

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

            await Dictionary.AddContentAsync([imageContentStream], _pdfEditor);

            _pdfEditor.Update(IndirectObject);
        }

        //// TODO: consider coordinate system enum.
        //// Should be consistent between this and text/image objects.
        //public void AddPath(Drawing.Path path)
        //{
        //    EnsureEditable();
        //    ArgumentNullException.ThrowIfNull(path);

        //    // TODO: PathObject
        //}

        public async Task RotateAsync(Rotation rotation)
        {
            // TODO: Ensure contents don't need some sort of transform to match

            ArgumentNullException.ThrowIfNull(rotation);

            var existingRotation = 0;
            
            if (Dictionary.Rotate != null)
            {
                existingRotation = await Dictionary.Rotate.GetAsync();
            }

            // The page may already be rotated, or inherit a value for rotation.
            // In practice, it is likely desired to rotate by a further n degrees.
            Dictionary.SetRotation(existingRotation + rotation);

            _pdfEditor.Update(IndirectObject);
        }

        // TODO: move to testable class (ICalulations maybe)
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
