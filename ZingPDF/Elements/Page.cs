using ZingPDF.Graphics.Images;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;

namespace ZingPDF.Elements
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public class Page
    {
        private readonly IPdf _pdf;

        internal Page(IndirectObject pageObject, IPdf pdf)
        {
            ArgumentNullException.ThrowIfNull(pageObject, nameof(pageObject));
            ArgumentNullException.ThrowIfNull(pdf);

            IndirectObject = pageObject;
            _pdf = pdf;
        }

        /// <summary>
        /// Gets the underlying indirect object for the page.
        /// </summary>
        public IndirectObject IndirectObject { get; }

        /// <summary>
        /// Gets the page dictionary for the page.
        /// </summary>
        public PageDictionary Dictionary => (PageDictionary)IndirectObject.Object;

        // TODO: AddImage and AddText use different conventions.
        // This method should accept the text plus options and construct the textobject internally...
        // or the image one should accept an image object like this one.
        /// <summary>
        /// Adds a text object to the page contents.
        /// </summary>
        public async Task AddTextAsync(TextObject text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var textContentStreamObject = await new ContentStreamFactory([text])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var textContentStreamIndirectObject = await _pdf.Objects.AddAsync(textContentStreamObject);

            await Dictionary.AddContentAsync(textContentStreamIndirectObject.Reference);

            _pdf.Objects.Update(IndirectObject);
        }

        /// <summary>
        /// Adds an image to the page contents.
        /// </summary>
        public async Task AddImageAsync(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            // TODO: Think about whether to implement inline images

            var imageMetadata = await new ImageSharpMetadataRetriever().GetAsync(image.ImageData);

            //List<IFilter> filters = [];
            var filter = imageMetadata.CompressionType.ToPDFFilterName();

            //ShorthandArrayObject? filterAry = filter != null ? new ShorthandArrayObject([(Name)filter]) : null;

            //if (filter != null)
            //{
            //    // TODO: derive required params, possibly only for CCITT compression.
            //    filters = [FilterFactory.Create(filter, null)];
            //}

            var imageDictionary = new ImageDictionary(
                _pdf,
                ObjectContext.UserCreated,
                imageMetadata.Width,
                imageMetadata.Height,
                imageMetadata.ColorSpace.ToString(),
                imageMetadata.BitDepth,
                filters: filter != null ? [(Name)filter] : null,
                decodeParms: null
                );

            imageDictionary.Set<Number>(Constants.DictionaryKeys.Stream.Length, image.ImageData.Length);

            var imageXObject = new StreamObject<ImageDictionary>(
                image.ImageData,
                imageDictionary,
                ObjectContext.UserCreated
                );

            var imageXObjectIndirectObject = await _pdf.Objects.AddAsync(imageXObject);

            var resourceName = UniqueStringGenerator.Generate();

            await Dictionary.AddXObjectResourceAsync(resourceName, imageXObjectIndirectObject.Reference, _pdf);

            var imageRect = image.MaxBounds;
            if (image.PreserveAspectRatio)
            {
                var (newWidth, newHeight) = ScaleToFit(imageMetadata.Width, imageMetadata.Height, image.MaxBounds.Width, image.MaxBounds.Height);

                imageRect = Syntax.CommonDataStructures.Rectangle.FromCoordinates(
                    image.MaxBounds.LowerLeft,
                    new Drawing.Coordinate(
                        image.MaxBounds.LowerLeft.X + newWidth,
                        image.MaxBounds.LowerLeft.Y + newHeight
                        )
                    );
            }

            var imageContentStreamObject = await new ContentStreamFactory([new ImageXObjectContentStream(resourceName, imageRect, ObjectContext.UserCreated)])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var imageContentStreamIndirectObject = await _pdf.Objects.AddAsync(imageContentStreamObject);

            await Dictionary.AddContentAsync(imageContentStreamIndirectObject.Reference);

            _pdf.Objects.Update(IndirectObject);
        }

        //// TODO: consider coordinate system enum.
        //// Should be consistent between this and text/image objects.
        //public void AddPath(Drawing.Path path)
        //{
        //    EnsureEditable();
        //    ArgumentNullException.ThrowIfNull(path);

        //    // TODO: PathObject
        //}

        /// <summary>
        /// Applies an additional rotation to the page.
        /// </summary>
        public async Task RotateAsync(Rotation rotation)
        {
            // TODO: Ensure contents don't need some sort of transform to match

            ArgumentNullException.ThrowIfNull(rotation);

            Rotation existingRotation = await Dictionary.Rotate.GetAsync() ?? Rotation.None;

            // The page may already be rotated, or inherit a value for rotation.
            // In practice, it is likely desired to rotate by a further n degrees.
            Dictionary.SetRotation(existingRotation + rotation);

            _pdf.Objects.Update(IndirectObject);
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
