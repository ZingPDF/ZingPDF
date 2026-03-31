using ZingPDF.Elements.Drawing;
using ZingPDF.Graphics.Images;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
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

        /// <summary>
        /// Adds a text object to the page contents.
        /// </summary>
        public async Task AddTextAsync(TextObject text)
        {
            ArgumentNullException.ThrowIfNull(text);

            await AddContentStreamAsync(text);
        }

        /// <summary>
        /// Adds text to the page using the provided bounds and font settings.
        /// </summary>
        public Task AddTextAsync(string text, Rectangle boundingBox, FontOptions fontOptions)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(boundingBox);
            ArgumentNullException.ThrowIfNull(fontOptions);

            return AddTextAsync(new TextObject(text, boundingBox, fontOptions));
        }

        /// <summary>
        /// Adds text to the page using a registered font.
        /// </summary>
        public async Task AddTextAsync(string text, Rectangle boundingBox, PdfFont font, Number size, Graphics.RGBColour colour)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(boundingBox);
            ArgumentNullException.ThrowIfNull(font);
            ArgumentNullException.ThrowIfNull(colour);

            await EnsureFontResourceAsync(font);
            await AddTextAsync(new TextObject(text, boundingBox, font, size, colour));
        }

        /// <summary>
        /// Adds an image to the page contents.
        /// </summary>
        public async Task AddImageAsync(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            var preparedImage = await ImageXObjectBuilder.CreateAsync(image.ImageData);
            var imageDictionary = CreateImageDictionary(preparedImage);

            if (preparedImage.SoftMask is not null)
            {
                var softMaskDictionary = CreateImageDictionary(preparedImage.SoftMask);
                var softMaskObject = new StreamObject<ImageDictionary>(
                    preparedImage.SoftMask.Data,
                    softMaskDictionary,
                    ObjectContext.UserCreated);
                var softMaskIndirectObject = await _pdf.Objects.AddAsync(softMaskObject);

                imageDictionary.Set(Constants.DictionaryKeys.Image.SMask, softMaskIndirectObject.Reference);
            }

            var imageXObject = new StreamObject<ImageDictionary>(
                preparedImage.Data,
                imageDictionary,
                ObjectContext.UserCreated);

            var imageXObjectIndirectObject = await _pdf.Objects.AddAsync(imageXObject);

            var resourceName = UniqueStringGenerator.Generate();

            await Dictionary.AddXObjectResourceAsync(resourceName, imageXObjectIndirectObject.Reference, _pdf);

            var imageRect = image.MaxBounds;
            if (image.PreserveAspectRatio)
            {
                var (newWidth, newHeight) = ScaleToFit(preparedImage.Width, preparedImage.Height, image.MaxBounds.Width, image.MaxBounds.Height);

                imageRect = Rectangle.FromCoordinates(
                    image.MaxBounds.LowerLeft,
                    new Coordinate(
                        image.MaxBounds.LowerLeft.X + newWidth,
                        image.MaxBounds.LowerLeft.Y + newHeight
                    )
                );
            }

            await AddContentStreamAsync(new ImageXObjectContentStream(resourceName, imageRect, ObjectContext.UserCreated));
        }

        /// <summary>
        /// Adds an image from a stream to the page contents.
        /// </summary>
        public Task AddImageAsync(Stream imageData, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            ArgumentNullException.ThrowIfNull(imageData);
            ArgumentNullException.ThrowIfNull(maxBounds);

            return AddImageAsync(new Image(imageData, maxBounds, preserveAspectRatio));
        }

        /// <summary>
        /// Adds an image from a file to the page contents.
        /// </summary>
        public async Task AddImageAsync(string imagePath, Rectangle maxBounds, bool preserveAspectRatio = true)
        {
            using var image = Image.FromFile(imagePath, maxBounds, preserveAspectRatio);
            await AddImageAsync(image);
        }

        /// <summary>
        /// Adds a drawable path to the page contents.
        /// </summary>
        public Task AddPathAsync(ZingPDF.Elements.Drawing.Path path)
        {
            ArgumentNullException.ThrowIfNull(path);

            return AddContentStreamAsync(new PathContentStream(path, ObjectContext.UserCreated));
        }

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

        private async Task AddContentStreamAsync(ContentStream contentStream)
        {
            var contentStreamObject = await new ContentStreamFactory([contentStream])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var contentStreamIndirectObject = await _pdf.Objects.AddAsync(contentStreamObject);

            await Dictionary.AddContentAsync(contentStreamIndirectObject.Reference);

            _pdf.Objects.Update(IndirectObject);
        }

        private ImageDictionary CreateImageDictionary(PreparedImageXObject preparedImage)
        {
            var filter = preparedImage.FilterName;
            var dictionary = new ImageDictionary(
                _pdf,
                ObjectContext.UserCreated,
                preparedImage.Width,
                preparedImage.Height,
                preparedImage.ColorSpace,
                preparedImage.BitsPerComponent,
                filters: filter != null ? [(Name)filter] : null,
                decodeParms: null);

            dictionary.Set<Number>(Constants.DictionaryKeys.Stream.Length, preparedImage.Data.Length);

            return dictionary;
        }

        private async Task EnsureFontResourceAsync(PdfFont font)
        {
            var resources = ResourceDictionary.FromDictionary(await Dictionary.Resources.GetAsync());
            await resources.AddFontAsync(font.ResourceName, font.FontReference, _pdf);
            Dictionary.SetResources(resources);
        }
    }
}
